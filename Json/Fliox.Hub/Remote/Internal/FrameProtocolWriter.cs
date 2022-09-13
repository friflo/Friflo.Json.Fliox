// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.Internal
{
    public class FrameProtocolWriter
    {
        private  readonly   byte[]  buffer = new byte[80];
        
        public async Task WriteAsync(
            NetworkStream           stream,
            ArraySegment<byte>      dataBuffer,
            WebSocketMessageType    messageType,
            bool                    endOfMessage,
            CancellationToken       cancellationToken)
        {
            int bufferPos = 0;
            if (endOfMessage) {
                byte opcode         = (byte)(messageType == WebSocketMessageType.Text ? Opcode.TextFrame : Opcode.BinaryFrame);
                byte frameFlags     = (byte)((byte)FrameFlags.Fin | opcode);
                bufferPos           = 1;
                buffer[0]           = frameFlags;
                var count           = dataBuffer.Count;
                // no masking in buffer[1] for now
                if (count < 126) {
                    bufferPos += 1;
                    buffer [1] = (byte)count;
                } else if (count <= 0xffff) {
                    bufferPos += 2;
                    buffer [1] = 126;
                    buffer [2] = (byte)(count & 0xff);
                    buffer [3] = (byte)(count >> 8);
                } else {
                    bufferPos += 8;
                    buffer [1] = 127;
                    buffer [2] = (byte) (count        & 0xff);
                    buffer [3] = (byte)((count >> 8)  & 0xff);
                    buffer [4] = (byte)((count >> 16) & 0xff);
                    buffer [5] = (byte)((count >> 24) & 0xff);
                    buffer [6] = 0;
                    buffer [7] = 0;
                    buffer [8] = 0;
                    buffer [9] = 0;
                }
            }
            

            await stream.WriteAsync(buffer, 0, bufferPos, cancellationToken);
        }
    }
}