// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.RFC6455
{
    internal sealed class FrameProtocol
    {
        public              bool                    EndOfMessage    { get; private set; }
        public              int                     ByteCount       => dataPos;
        public              WebSocketMessageType    MessageType     { get; private set; }
        
        private  readonly   byte[]                  buffer = new byte[4096];
        private             Parse                   parseState;
        private             int                     payloadLenBytes;
        private             int                     payloadLenPos;
        private             int                     maskingKeyPos;
        private             int                     dataPos;
        // --- Base Framing Protocol
        private             Fin                     fin;
        private             Rsv1                    rsv1;
        private             Rsv2                    rsv2;
        private             Rsv3                    rsv3;
        private             Opcode                  opcode;
        private             Mask                    mask;
        private             long                    payloadLen;
        private             int                     maskingKey;
        private             long                    payloadPos;

        internal async Task ReadFrame(NetworkStream stream, ArraySegment<byte> dataBuffer, CancellationToken cancellationToken) {
            EndOfMessage = false;
            while (true) {
                int count   = await stream.ReadAsync(buffer, cancellationToken);
                if (!Process(count, dataBuffer)) {
                    continue;
                }
                var debugStr = Encoding.UTF8.GetString(buffer, 0, dataPos);
                EndOfMessage = true;
                return;
            }
        }

        private bool Process (int count, ArraySegment<byte> dataBuffer) {
            int pos = 0;
            while (pos < count) {
                var b =  buffer[pos++];
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
                        maskingKey      = 0;
                        maskingKeyPos   = 0;
                        dataPos         = 0;
                        payloadPos      = 0;
                        parseState      = mask == Mask.Set ? Parse.Masking : Parse.Payload;
                        break;
                    case Parse.Masking:
                        maskingKey  = (maskingKey << 8) | b;
                        if (++maskingKeyPos < 4) {
                            break;
                        }
                        dataPos         = 0;
                        payloadPos      = 0;
                        parseState      = Parse.Payload;
                        break;
                    case Parse.Payload:
                        if (dataPos == 71) {
                            int i = 1;
                        }
                        dataBuffer[dataPos++] = b;
                        if (++payloadPos < payloadLen) {
                            break;
                        }
                        parseState = Parse.Opcode;
                        return true;
                }
            }
            return false;
        }

        private enum Parse {
            Opcode,
            PayloadLen,
            Masking,
            Payload,
        }
    }
}