// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    public sealed class FrameProtocolReader
    {
        public              bool                    EndOfMessage    { get; private set; }
        public              int                     ByteCount       => dataBufferPos;
        public              WebSocketMessageType    MessageType     { get; private set; }
        /// <summary> store the bytes read from the socket.
        /// <see cref="bufferPos"/> is its read position and <see cref="bufferLen"/> the count of bytes read from socket</summary>
        private  readonly   byte[]                  buffer;
        private             int                     bufferPos;
        private             int                     bufferLen;
        private             long                    processedByteCount;
        /// <summary> general <see cref="parseState"/> and its sub states <see cref="payloadLenPos"/> and <see cref="maskingKeyPos"/> </summary>
        private             Parse                   parseState;
        private             int                     payloadLenBytes;
        private             int                     payloadLenPos;
        private             int                     maskingKeyPos;
        /// <summary>write position of given <see cref="dataBuffer"/> </summary>
        private             int                     dataBufferPos;  // position in given dataBuffer
        private             ArraySegment<byte>      dataBuffer;
        private             int                     dataBufferLen;
        /// <summary> <see cref="payloadPos"/> read position payload. Increments up to <see cref="payloadLen"/> </summary>
        private             long                    payloadPos;
        private             long                    payloadLen;
        // --- Base Framing Protocol headers
        private             bool                    mask;
        private readonly    byte[]                  maskingKey = new byte[4];
        
        public FrameProtocolReader(int bufferSize = 4096) {
            buffer = new byte[bufferSize];
        }

        public async Task ReadFrame(Stream stream, ArraySegment<byte> dataBuffer, CancellationToken cancellationToken)
        {
            dataBufferLen   = dataBuffer.Count;
            this.dataBuffer = dataBuffer;
            dataBufferPos   = 0;
            while (true) {
                // process unprocessed bytes in buffer from previous call
                if (Process()) {
                    // var debugStr = Encoding.UTF8.GetString(dataBuffer.Array, 0, dataPos);
                    return;
                }
                bufferPos = 0;
                bufferLen = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bufferLen < 1) throw new InvalidOperationException("FrameProtocolReader expect ReadAsync() return len > 0");
            }
        }
        
        /// general state of state machine 
        private enum Parse {
            Opcode,
            PayloadLen,
            Masking,
            Payload,
        }

        private bool Process ()
        {
            // performance: use locals enable CPU using these values from stack
            var buf         = buffer;
            var len         = bufferLen;
            var startPos    = bufferPos;
            
            while (bufferPos < len) {
                var b =  buf[bufferPos++];
                switch (parseState) {
                    case Parse.Opcode:
                        var flags       = (FrameFlags)b;
                        var opcode      = (Opcode)   (b & (int)FrameFlags.Opcode);
                        MessageType     = GetMessageType(opcode);
                        EndOfMessage    = (flags & FrameFlags.Fin) != 0;
                        payloadLenPos   = -1;
                        parseState      = Parse.PayloadLen;
                        break;
                    case Parse.PayloadLen:
                        if (payloadLenPos == -1) {
                            mask            = (b & (int)LenFlags.Mask) != 0; 
                            payloadLen      = b & 0x7f;
                            payloadLenPos   = 0;
                            if (payloadLen == 126) {
                                payloadLen      = 0;
                                payloadLenBytes = 2;
                                break;
                            }
                            if (payloadLen == 127) {
                                payloadLen      = 0;
                                payloadLenBytes = 8;
                                break;
                            }
                        } else {
                            payloadLen += b << 8 * payloadLenPos;
                            if (++payloadLenPos < payloadLenBytes)
                                break;
                        }
                        maskingKeyPos   = 0;
                        payloadPos      = 0;
                        parseState      = mask ? Parse.Masking : Parse.Payload;
                        break;
                    case Parse.Masking:
                        maskingKey[maskingKeyPos++] = b;
                        if (maskingKeyPos < 4) {
                            break;
                        }
                        payloadPos      = 0;
                        parseState      = Parse.Payload;
                        break;
                    case Parse.Payload:
                        // if (dataPos == 71) { int debug = 1; }
                        if (mask) {
                            var j = dataBufferPos % 4;
                            dataBuffer[dataBufferPos++] = (byte)(b ^ maskingKey[j]);
                        } else {
                            dataBuffer[dataBufferPos++] = b;
                        }
                        if (++payloadPos < payloadLen) {
                            break;
                        }
                        parseState          = Parse.Opcode;
                        processedByteCount += bufferPos - startPos;
                        return true;
                }
            }
            processedByteCount += bufferPos - startPos;
            return false;
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