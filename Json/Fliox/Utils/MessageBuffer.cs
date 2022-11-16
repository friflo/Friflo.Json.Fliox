// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Utils
{
    public readonly struct MessageBuffer
    {
        private  readonly   byte[]  buffer;
        private  readonly   int     start;
        private  readonly   int     len;
        
        internal            bool    IsNull()        => buffer == null;
        public   override   string  ToString()      => AsString();
        public              string  AsString()      => buffer == null ? "null" : Encoding.UTF8.GetString(buffer, start, len);
        public  ArraySegment<byte>  AsArraySegment()=> new ArraySegment<byte>(buffer, start, len);
        
        internal MessageBuffer(byte[] buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
    }
    
    public enum MessageBufferEvent {
        Closed      = 1,
        NewMessage  = 2,
    }
    
    public class MessageBufferQueue : IDisposable
    {
        private             byte[]                  buffer0;
        private             int                     buffer0Pos;
        private             byte[]                  buffer1;
        private             int                     buffer1Pos;
        
        private             int                     writeBuffer; // 0 or 1
        
        private             byte[]                  Buffer   => writeBuffer == 0 ? buffer0 : buffer1;
        private             ref int BufferPos       { get { if (writeBuffer == 0) return ref buffer0Pos; return ref buffer1Pos; } }

        private  readonly   List<MessageBuffer>     queue;
        
        private             bool                    closed;
        private  readonly   SemaphoreSlim           messageAvailable;
        
        public MessageBufferQueue(int capacity = 128) {
            buffer0             = new byte[capacity];
            buffer1             = new byte[capacity];
            queue               = new List<MessageBuffer>();
            
            messageAvailable    = new SemaphoreSlim(0, 1);
        }

        public void Dispose() {
            messageAvailable.Dispose();
        }
        
        public void Enqueue(in JsonValue data) {
            Enqueue(data.GetArrayMutable(), 0, data.Length);
        }
        
        public void Enqueue(in Bytes bytes) {
            Enqueue(bytes.buffer.array, bytes.start, bytes.Len);
        }

        private void Enqueue(byte[] data, int start, int len) {
            if (closed) throw new InvalidOperationException("MessageBufferQueue already closed");
                
            lock (queue) {
                var buffer          = Buffer;
                var bufferLen       = buffer.Length;
                ref var bufferPos   = ref BufferPos;
                var remaining       = bufferLen - bufferPos;
                if (len > remaining) {
                    bufferLen   = Math.Max(2 * bufferLen, len);
                    buffer      = new byte[bufferLen];
                    if (writeBuffer == 0) {
                        buffer0 = buffer;
                    } else {
                        buffer1 = buffer;
                    }
                    BufferPos = 0;
                }
                System.Buffer.BlockCopy(data, start, buffer, bufferPos, len);
                var message = new MessageBuffer(buffer, bufferPos, len);
                queue.Add(message);
                BufferPos += len;
                
                // send event _after_ adding message to queue
                if (messageAvailable.CurrentCount == 0) {
                    messageAvailable.Release();
                }
            }
        }
        
        public async Task<MessageBufferEvent> DequeMessages(List<MessageBuffer> messages)
        {
            messages.Clear();
            await messageAvailable.WaitAsync().ConfigureAwait(false);

            lock (queue) {
                foreach (var message in queue) {
                    messages.Add(message);                    
                }
                queue.Clear();
                writeBuffer = writeBuffer == 0 ? 1 : 0;
                return closed ? MessageBufferEvent.Closed : MessageBufferEvent.NewMessage;
            }
        }
        
        public void FreeDequeuedMessages() {
            lock (queue) {
                if (writeBuffer == 0) {
                    buffer1Pos = 0; 
                } else {
                    buffer0Pos = 0;
                }
            }
        }

        public void Close() {
            lock (queue) {
                closed = true;
                if (messageAvailable.CurrentCount == 0) {
                    messageAvailable.Release();
                }
            }
        }
    }
}