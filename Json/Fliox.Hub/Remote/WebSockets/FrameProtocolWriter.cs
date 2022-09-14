// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    public sealed class FrameProtocolWriter
    {
        private  readonly   byte[]  buffer = new byte[80];
        
        public async Task WriteAsync(
            Stream                  stream,
            ArraySegment<byte>      dataBuffer,
            WebSocketMessageType    messageType,
            bool                    endOfMessage,
            CancellationToken       cancellationToken)
        {
            int count       = dataBuffer.Count;
            int bufferPos   = WriteHeader(count, messageType, endOfMessage, buffer);
            
            dataBuffer.CopyTo(buffer, bufferPos);
            bufferPos += dataBuffer.Count;

            await stream.WriteAsync(buffer, 0, bufferPos, cancellationToken);
        }
        
        private static int WriteHeader(int count, WebSocketMessageType  messageType, bool endOfMessage, byte[] buffer)
        {
            var opcode      = (byte)(messageType == WebSocketMessageType.Text ? Opcode.TextFrame : Opcode.BinaryFrame);
            var fin         = (byte)(endOfMessage ? FrameFlags.Fin : 0);
            buffer[0]       = (byte)(fin | opcode);
            int  bufferPos  = 1;
            // no masking in buffer[1] for now
            if (count < 126) {
                bufferPos += 1;
                buffer [1] = (byte)count;
            } else if (count <= 0xffff) {
                bufferPos += 3;
                buffer [1] = 126;
                buffer [2] = (byte) (count & 0xff);
                buffer [3] = (byte) (count >> 8);
            } else {
                bufferPos += 9;
                buffer [1] = 127;
                buffer [2] = (byte) (count        & 0xff);
                buffer [3] = (byte)((count >>  8) & 0xff);
                buffer [4] = (byte)((count >> 16) & 0xff);
                buffer [5] = (byte)((count >> 24) & 0xff);
                buffer [6] = 0;
                buffer [7] = 0;
                buffer [8] = 0;
                buffer [9] = 0;
            }
            return bufferPos;
        }
    }
}