// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Utils
{
    public readonly struct MessageBuffer
    {
        private  readonly   byte[]  buffer;
        private  readonly   int     start;
        private  readonly   int     len;
        
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
    
    /// <summary>
    /// A queue to store messages using double buffering to avoid frequent allocations of byte arrays for each message. <br/>
    /// One buffer is used to store newly enqueued messages. <br/>
    /// The other buffer is used to read dequeued messages. <br/> 
    /// </summary>
    public class MessageBufferQueue : IDisposable
    {
        private             byte[]                  buffer0;
        private             int                     buffer0Pos;
        private             byte[]                  buffer1;
        private             int                     buffer1Pos;
        
        private             int                     writeBuffer; // 0 or 1
        
        private             byte[]                  Buffer         => writeBuffer == 0 ? buffer0 : buffer1;
        private             int                     GetBufferPos() => writeBuffer == 0 ? buffer0Pos : buffer1Pos;
        private             void                    SetBufferPos(int pos) { if (writeBuffer == 0) buffer0Pos = pos; else buffer1Pos = pos; }

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
            var array   = data.Array;
            var start   = data.Start;
            var len     = data.Count;
            Enqueue(array, start, len);
        }

        private void Enqueue(byte[] data, int start, int len) {
            lock (queue) {
                if (closed) {
                    throw new InvalidOperationException("MessageBufferQueue already closed");
                }
                var buffer          = Buffer;
                var bufferLen       = buffer.Length;
                var bufferPos       = GetBufferPos();
                var remaining       = bufferLen - bufferPos;
                if (len > remaining) {
                    bufferLen   = Math.Max(2 * bufferLen, len);
                    buffer      = new byte[bufferLen];
                    if (writeBuffer == 0) {
                        buffer0 = buffer;
                    } else {
                        buffer1 = buffer;
                    }
                    SetBufferPos(0);
                    bufferPos = 0;
                }
                System.Buffer.BlockCopy(data, start, buffer, bufferPos, len);
                var message = new MessageBuffer(buffer, bufferPos, len);
                queue.Add(message);
                SetBufferPos(bufferPos + len);
            }
            // send event _after_ adding message to queue
            if (messageAvailable.CurrentCount == 0) {
                messageAvailable.Release();
            }
        }
        
        public async Task<MessageBufferEvent> DequeMessages(List<MessageBuffer> messages)
        {
            messages.Clear();
            await messageAvailable.WaitAsync().ConfigureAwait(false);

            lock (queue) {
                // swap read & write buffer.
                writeBuffer = writeBuffer == 0 ? 1 : 0;
                // newly enqueued messages are written to the head of the write buffer
                SetBufferPos(0);
                foreach (var message in queue) {
                    messages.Add(message);                    
                }
                queue.Clear();

                return closed ? MessageBufferEvent.Closed : MessageBufferEvent.NewMessage;
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