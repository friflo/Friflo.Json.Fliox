// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    public sealed class FrameProtocolWriter
    {
        private  readonly   byte[]  writeBuffer;
        private  readonly   int     maxBufferSize;
        private  readonly   bool    mask;
        private  const      int     MaxHeaderLength = 14; // opcode: 1 + payload length: 9 + mask: 4
        
        public FrameProtocolWriter(bool mask, int bufferSize = 4096) {
            writeBuffer     = new byte[bufferSize + MaxHeaderLength];
            maxBufferSize   = bufferSize;
            this.mask       = mask;
        }
        
        /// [RFC 6455: The WebSocket Protocol - Close] https://www.rfc-editor.org/rfc/rfc6455#section-5.5.1
        public async Task CloseAsync(Stream stream, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            var description = Encoding.UTF8.GetBytes(statusDescription);
            var response    = new byte[2 + description.Length];
            response[0]     = (byte)((int)closeStatus >> 8);
            response[1]     = (byte)((int)closeStatus & 0xff);
            Buffer.BlockCopy(description, 0, response, 2, description.Length);

            await WriteAsync(stream, response, WebSocketMessageType.Close, true, cancellationToken).ConfigureAwait(false);
        }
        
        public async Task WriteAsync(
            Stream                  stream,
            byte[]                  dataBuffer,
            WebSocketMessageType    messageType,
            bool                    endOfMessage,
            CancellationToken       cancellationToken)
        {
            var buffer      = writeBuffer; // performance: use local enable CPU using these value from stack
            if (dataBuffer == null) throw new InvalidOperationException("expect dataBuffer array not null");
            int dataCount   = dataBuffer.Length;
            int remaining   = dataCount;
            int dataPos     = 0;
            
            while (true) {
                // if message > max buffer size write multiple fragments
                var isLast      = remaining <= maxBufferSize;
                var writeCount  = isLast ? remaining : maxBufferSize;
                var maskingKey  = mask ? new byte [] { 1, 2, 3, 4 } : null;
                
                int bufferLen   = WriteHeader(writeCount, messageType, isLast, buffer, maskingKey);
                
                // append writeCount bytes from message to buffer
                if (maskingKey != null) {
                    for (int n = 0; n < writeCount; n++) {
                        var dataIndex = dataPos + n;
                        var j = dataIndex % 4;
                        var b = dataBuffer[dataIndex];
                        buffer[bufferLen + n] = (byte)(b ^ maskingKey[j]);
                    }
                } else {
                    Buffer.BlockCopy(dataBuffer, dataPos, buffer, bufferLen, writeCount);
                }
                bufferLen   += writeCount;
                remaining   -= writeCount;
                dataPos     += writeCount;

                await stream.WriteAsync(buffer, 0, bufferLen, cancellationToken).ConfigureAwait(false);
                
                if (remaining <= 0)
                    break;
            }
        }
        
        private static int WriteHeader(
            int                     count,
            WebSocketMessageType    messageType,
            bool                    endOfMessage,
            byte[]                  buffer,
            byte[]                  maskingKey)
        {
            // --- write Fin & Opcode
            var opcode      = (byte)GetOpcode(messageType);
            var fin         = (byte)(endOfMessage ? FrameFlags.Fin : 0);
            buffer[0]       = (byte)(fin | opcode);
            var lenMask     = maskingKey == null ? 0 : (int)LenFlags.Mask;
            int  bufferPos  = 1;
            
            // --- write payload length. It uses network byte order (big endian). E.g 0x0102 -> byte[] { 01, 02 }
            if (count < 126) {
                bufferPos += 1;
                buffer [1] = (byte)(count | lenMask);
            } else if (count <= 0xffff) {
                bufferPos += 3;
                buffer [1] = (byte)(126 | lenMask);
                buffer [2] = (byte) (count >> 8);
                buffer [3] = (byte) (count & 0xff);
            } else {
                bufferPos += 9;
                buffer [1] = (byte)(127 | lenMask);
                buffer [2] = 0;
                buffer [3] = 0;
                buffer [4] = 0;
                buffer [5] = 0;
                buffer [6] = (byte)((count >> 24) & 0xff);
                buffer [7] = (byte)((count >> 16) & 0xff);
                buffer [8] = (byte)((count >>  8) & 0xff);
                buffer [9] = (byte) (count        & 0xff);
            }
            
            // --- write masking key
            if (maskingKey != null) {
                buffer [bufferPos]     = maskingKey[0];
                buffer [bufferPos + 1] = maskingKey[1];
                buffer [bufferPos + 2] = maskingKey[2];
                buffer [bufferPos + 3] = maskingKey[3];
                bufferPos += 4;
            }
            return bufferPos;
        }
        
        private static Opcode GetOpcode(WebSocketMessageType messageType) {
            switch(messageType) {
                case WebSocketMessageType.Text:     return Opcode.TextFrame;
                case WebSocketMessageType.Binary:   return Opcode.BinaryFrame;
                case WebSocketMessageType.Close: 
                default:                            return Opcode.ConnectionClose;
            }
        }
    }
}