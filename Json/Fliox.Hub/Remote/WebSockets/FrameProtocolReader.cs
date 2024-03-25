// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Burst.Vector;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    /// <summary>
    /// WebSocket stream parser implemented using a final state machine. <br/>
    /// Parses a WebSocket (without masking, message with length 208 bytes) stream with 2.5 GB/sec on a Intel(R) Core(TM) i7-4790K CPU 4.00GHz <br/>
    /// </summary>
    /// <remarks>
    /// Objectives of implementation:<br/>
    /// - Support <see cref="Stream.ReadAsync(byte[],int,int)"/> returning only a single byte at all states<br/>
    /// - Maximize memory locality by using a fixed size <see cref="buffer"/><br/>
    /// - Minimize calls to <see cref="Stream.ReadAsync(byte[],int,int)"/> <br/>
    /// - Minimize conditions. Especially in hot loops e.g. when reading the payload <see cref="ReadPayload"/><br/>
    /// - Minimize heap allocations. The only allocations are
    /// <list type="bullet">
    ///   <item>the Task when calling <see cref="ReadFrame"/></item>
    ///   <item>the Task when calling <see cref="Stream.ReadAsync(byte[],int,int)"/> no more bytes left ro read in <see cref="buffer"/></item>
    /// </list>
    /// </remarks> 
    public sealed class FrameProtocolReader
    {
        public              bool                    EndOfMessage            { get; private set; }
        public              int                     ByteCount               => dataBufferPos;
        public              WebSocketMessageType    MessageType             { get; private set; }
        public              WebSocketCloseStatus?   CloseStatus             { get; private set; }
        public              WebSocketState          SocketState             { get; private set; }
        public              string                  CloseStatusDescription  { get; private set; }
        public              long                    ProcessedByteCount      { get; private set; }
        /// <summary> store the bytes read from the socket.
        /// <see cref="bufferPos"/> is its read position and <see cref="bufferLen"/> the count of bytes read from socket</summary>
        private  readonly   byte[]                  buffer;
        private             int                     bufferPos;
        private             int                     bufferLen;
        private             int                     BufferRest              => bufferLen - bufferPos;
        /// <summary> general <see cref="frameState"/> and its sub states <see cref="payloadLenPos"/> and <see cref="maskingKeyPos"/> </summary>
        private             FrameState              frameState;
        private             bool                    fin;
        private             Opcode                  opcode;
        private             int                     payloadLenBytes;
        private             int                     payloadLenPos;
        private             int                     maskingKeyPos;
        /// <summary>write position of given <see cref="dataBuffer"/> </summary>
        private             int                     dataBufferPos;
        private             byte[]                  dataBuffer;
        /// <summary>[RFC 6455: The WebSocket Protocol - Control Frames] https://www.rfc-editor.org/rfc/rfc6455#section-5.5.1 </summary>
        private readonly    byte[]                  controlFrameBuffer;
        private             int                     controlFrameBufferPos;
        /// <summary> <see cref="payloadPos"/> read position payload. Increments up to <see cref="payloadLen"/> </summary>
        private             long                    payloadPos;
        private             long                    payloadLen;
        // --- Base Framing Protocol headers
        private             bool                    mask;
        private readonly    byte[]                  maskingKey = new byte[20];
        
        public FrameProtocolReader(int bufferSize = 4096) {
            buffer              = new byte[bufferSize];
            SocketState         = WebSocketState.Open;
            controlFrameBuffer  = new byte[125];
        }

        public async Task<WebSocketState> ReadFrame(Stream stream, byte[] dataBuffer, CancellationToken cancellationToken)
        {
            if (SocketState != WebSocketState.Open) throw new InvalidOperationException("reader already closed");
            this.dataBuffer = dataBuffer;
            dataBufferPos   = 0;
            while (true) {
                // process unprocessed bytes in buffer from previous call
                var  startPos       = bufferPos;
                bool frameEnd       = ProcessFrame();
                ProcessedByteCount += bufferPos - startPos;
                
                if (frameEnd) {
                    // var debugStr = Encoding.UTF8.GetString(dataBuffer.Array, 0, ByteCount);
                    return SocketState;
                }
                bufferPos = 0;
                try {
                    bufferLen = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    // ReadAsync() return 0 if end of the stream has been reached
                    CloseStatusDescription  = "reached end of stream";
                    if (bufferLen > 0)
                        continue;
                }
                catch (IOException e) {
                    var innerException = e.InnerException;
                    CloseStatusDescription = innerException != null ? innerException.Message : e.Message;
                }
                SocketState             = WebSocketState.Closed;
                CloseStatus             = WebSocketCloseStatus.EndpointUnavailable;
                MessageType             = WebSocketMessageType.Close;
                EndOfMessage            = true;
                return SocketState;
            }
        }
        
        /// <summary>
        /// return true:  if reading payload is complete or the given <see cref="dataBuffer"/> is filled
        /// return false: if more frame bytes need to be read
        /// </summary>
        private bool ProcessFrame ()
        {
            // performance: use locals enable CPU using these values from stack
            var buf     = buffer;
            var len     = bufferLen;

            while (bufferPos < len) {
                byte b;
                switch (frameState) {
                    case FrameState.Opcode: {           // --- 1 byte
                        b               =  buf[bufferPos++];
                        fin             =          (b & (int)FrameFlags.Fin) != 0;
                        opcode          = (Opcode) (b & (int)FrameFlags.Opcode);
                        MessageType     = GetMessageType(opcode);
                        frameState      = FrameState.PayloadLenStart;
                        break;
                    }
                    case FrameState.PayloadLenStart: {  // --- 1 byte
                        b               = buf[bufferPos++];
                        mask            = (b & (int)LenFlags.Mask) != 0; 
                        var length      = b & (int)LenFlags.PayloadLength;
                        if (length < 126) {
                            payloadLen = length;
                            if (MaskOrPayloadTransition())
                                return true;
                            break;
                        }
                        payloadLen      = 0;
                        payloadLenPos   = 0;
                        frameState      = FrameState.PayloadLen;
                        if (length == 126) {
                            payloadLenBytes = 2;
                            break;
                        }
                        // length == 127
                        payloadLenBytes = 8;
                        break;
                    }
                    case FrameState.PayloadLen: {       // --- 2 or 8 payloadLenBytes bytes
                        var minRest = Math.Min(payloadLenBytes - payloadLenPos, BufferRest);
                        for (int n = 0; n < minRest; n++) {
                            b = buf[bufferPos + n];
                            // payload length uses network byte order (big endian). E.g 0x0102 -> byte[] { 01, 02 }
                            payloadLen = (payloadLen << 8) | b;
                        }
                        bufferPos       += minRest;
                        payloadLenPos   += minRest;
                        if (payloadLenPos < payloadLenBytes)
                            return false;
                        if (MaskOrPayloadTransition())
                            return true;
                        break;
                    }
                    case FrameState.Masking: {          // --- 4 bytes
                        var minRest = Math.Min(4 - maskingKeyPos, BufferRest);
                        for (int n = 0; n < minRest; n++) {
                            b = buf[bufferPos + n];
                            maskingKey[maskingKeyPos + n] = b;
                        }
                        bufferPos       += minRest;
                        maskingKeyPos   += minRest;
                        if (maskingKeyPos < 4) {
                            break;
                        }
                        VectorOps.Instance.Populate(maskingKey);
                        if (PayloadTransition())
                            break;
                        return true; // empty payload
                    }
                    case FrameState.Payload: {          // --- payloadLen bytes
                        var dataBufferStart = dataBufferPos;
                        var payloadResult   = ReadPayload();

                        if (opcode == Opcode.ConnectionClose) {
                            UpdateControlFrameBuffer(dataBufferStart);
                        }
                        return payloadResult;
                    }
                }
            }
            return false;
        }
        
        private bool MaskOrPayloadTransition() {
            maskingKeyPos   = 0;
            if (mask) {
                frameState  = FrameState.Masking;
                return false;
            }
            if (PayloadTransition())
                return false;
            return true; // empty payload
        }
        
        private bool PayloadTransition() {
            controlFrameBufferPos   = 0;
            if (payloadLen > 0) {
                payloadPos  = 0;
                frameState  = FrameState.Payload;
                return true;
            }
            // payloadLen == 0
            frameState      = FrameState.Opcode;
            EndOfMessage    = fin;
            if (opcode == Opcode.ConnectionClose) {
                UpdateControlFrameBuffer(dataBufferPos);
            }
            return false;
        }
        
        /// <summary>
        /// return true:  if reading payload is complete or the given <see cref="dataBuffer"/> is filled.
        /// return false: if more payload bytes need to be read
        /// </summary>
        private bool ReadPayload()
        {
            var dataBufferLen   = dataBuffer.Length;
            
            var payloadRest     = payloadLen    - payloadPos; 
            var dataBufferRest  = dataBufferLen - dataBufferPos;
            
            var minRest = Math.Min((int)payloadRest, dataBufferRest);
            minRest     = Math.Min(minRest,BufferRest);
            if (mask) {
                VectorOps.Instance.Xor(dataBuffer, dataBufferPos, buffer, bufferPos, maskingKey, (int)payloadPos, minRest);
            } else {
                Buffer.BlockCopy(buffer, bufferPos, dataBuffer, dataBufferPos, minRest);
            }
            // --- update states ---
            bufferPos       += minRest;
            dataBufferPos   += minRest;
            payloadPos      += minRest;

            if (payloadPos == payloadLen) {
                frameState      = FrameState.Opcode;
                EndOfMessage    = fin;
                return true;
            }
            if (dataBufferPos == dataBufferLen) {
                EndOfMessage    = false;
                return true;
            }
            return false;
        }
        
        
        private void UpdateControlFrameBuffer(int dataBufferStart) {
            var bytesAdded = dataBufferPos - dataBufferStart;
            Buffer.BlockCopy(dataBuffer, dataBufferStart, controlFrameBuffer, controlFrameBufferPos, bytesAdded);
            controlFrameBufferPos += bytesAdded;
            if (!EndOfMessage)
                return;
            // [RFC 6455: The WebSocket Protocol - Close] https://www.rfc-editor.org/rfc/rfc6455#section-5.5.1
            SocketState             = WebSocketState.CloseReceived;
            if (controlFrameBufferPos >= 2) {
                CloseStatus             = (WebSocketCloseStatus)(controlFrameBuffer[0] << 8 | controlFrameBuffer[1]);
                CloseStatusDescription  = Encoding.UTF8.GetString(controlFrameBuffer, 2, controlFrameBufferPos - 2);
            } else {
                CloseStatus             = WebSocketCloseStatus.NormalClosure;
                CloseStatusDescription  = "";
            }
        }
        
        private static WebSocketMessageType GetMessageType(Opcode opcode) {
            switch(opcode) {
                case Opcode.TextFrame:      return WebSocketMessageType.Text;
                case Opcode.BinaryFrame:    return WebSocketMessageType.Binary;
                default:                    return WebSocketMessageType.Close;
            }
        }
    }
}