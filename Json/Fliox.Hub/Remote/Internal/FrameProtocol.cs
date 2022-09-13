// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.RFC6455
{
    internal sealed class FrameProtocol
    {
        public              bool                    EndOfMessage    { get; private set; }
        public              int                     ByteCount       => dataPos;
        public              WebSocketMessageType    MessageType     { get; private set; }
        /// <summary> store the bytes read from the socket.
        /// <see cref="bufferPos"/> is its read position and <see cref="bufferLen"/> the count of bytes read from socket</summary>
        private  readonly   byte[]                  buffer = new byte[4096];
        private             int                     bufferPos;
        private             int                     bufferLen;
        /// <summary> general <see cref="parseState"/> and its sub states <see cref="payloadLenPos"/> and <see cref="maskingKeyPos"/> </summary>
        private             Parse                   parseState;
        private             int                     payloadLenBytes;
        private             int                     payloadLenPos;
        private             int                     maskingKeyPos;
        /// <summary>write position of given <see cref="dataBuffer"/> </summary>
        private             int                     dataPos;        // position in given dataBuffer
        private             ArraySegment<byte>      dataBuffer;
        /// <summary> <see cref="payloadPos"/> read position payload. Increments up to <see cref="payloadLen"/> </summary>
        private             long                    payloadPos;
        private             long                    payloadLen;
        // --- Base Framing Protocol headers
        private             Fin                     fin;
        private             Rsv1                    rsv1;
        private             Rsv2                    rsv2;
        private             Rsv3                    rsv3;
        private             Opcode                  opcode;
        private             Mask                    mask;
        private readonly    byte[]                  maskingKey = new byte[4];

        internal async Task ReadFrame(NetworkStream stream, ArraySegment<byte> dataBuffer, CancellationToken cancellationToken)
        {
            dataPos         = 0;
            this.dataBuffer = dataBuffer;
            while (true) {
                // process unprocessed bytes in buffer from previous call
                if (Process()) {
                    // var debugStr = Encoding.UTF8.GetString(dataBuffer.Array, 0, dataPos);
                    return;
                }
                bufferLen = await stream.ReadAsync(buffer, cancellationToken);
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
            // performance: use locals enable CPU using their values from stack 
            var buf = buffer;
            var len = bufferLen;
            
            while (bufferPos < len) {
                var b =  buf[bufferPos++];
                switch (parseState) {
                    case Parse.Opcode:
                        fin             = (Fin)     ((b >> 7) & 0x1);
                        rsv1            = (Rsv1)    ((b >> 6) & 0x1);
                        rsv2            = (Rsv2)    ((b >> 5) & 0x1);
                        rsv3            = (Rsv3)    ((b >> 4) & 0x1);
                        opcode          = (Opcode)   (b       & 0xf);
                        payloadLenPos   = 0;
                        parseState      = Parse.PayloadLen;
                        break;
                    case Parse.PayloadLen:
                        if (payloadLenPos == 0) {
                            mask            = (Mask)((b >> 7) & 0x1);
                            payloadLen      = b & 0x7f;
                            payloadLenPos   = 1;
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
                            payloadLen = (payloadLen << 8) | b;
                            if (++payloadLenPos <= payloadLenBytes)
                                break;
                        }
                        maskingKeyPos   = 0;
                        payloadPos      = 0;
                        parseState      = mask == Mask.Set ? Parse.Masking : Parse.Payload;
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
                        var j = dataPos % 4;
                        dataBuffer[dataPos++] = (byte)(b ^ maskingKey[j]);
                        if (++payloadPos < payloadLen) {
                            break;
                        }
                        EndOfMessage    = fin == Fin.Final;
                        MessageType     = GetMessageType(opcode);
                        parseState      = Parse.Opcode;
                        return true;
                }
            }
            return false;
        }
        
        private static WebSocketMessageType GetMessageType(Opcode opcode)
        {
            return opcode switch {
                Opcode.TextFrame    => WebSocketMessageType.Text,
                Opcode.BinaryFrame  => WebSocketMessageType.Binary,
                _                   => WebSocketMessageType.Close
            };
        }
    }
}