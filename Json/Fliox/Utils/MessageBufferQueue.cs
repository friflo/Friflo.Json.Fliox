// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Utils
{
    public enum MessageBufferEvent {
        Closed      = 1,
        NewMessage  = 2,
    }
    
    public readonly struct MessageItem<T>
    {
        public readonly JsonValue value;
        public readonly T         meta;
        
        internal MessageItem (in JsonValue value, T meta) {
            this.value  = value;
            this.meta   = meta;
        }
    }
    
    public readonly struct VoidMeta { }

    /// <summary>
    /// A queue to store messages using double buffering to avoid frequent allocations of byte arrays for each message. <br/>
    /// One buffer is used to store newly enqueued messages. <br/>
    /// The other buffer is used to read dequeued messages. <br/>
    /// </summary>
    /// <typeparam name="TMeta">type of the meta data associated to each message.
    /// Use <see cref="VoidMeta"/> if no associated is required</typeparam>
    public sealed class MessageBufferQueue<TMeta>
    {
        private             byte[]          buffer0;
        private             int             buffer0Pos;
        private             byte[]          buffer1;
        private             int             buffer1Pos;
        
        private             int             writeBuffer; // 0 or 1
        
        private             byte[]          Buffer          => writeBuffer == 0 ? buffer0 : buffer1;
        private             int             GetBufferPos()  => writeBuffer == 0 ? buffer0Pos : buffer1Pos;
        private             void            SetBufferPos(int pos) { if (writeBuffer == 0) buffer0Pos = pos; else buffer1Pos = pos; }
        
        public              int             Count           => queue.Count;

        private  readonly   List<MessageItem<TMeta>> queue;
        
        private             bool            closed;

        
        public MessageBufferQueue(int capacity = 128) {
            buffer0             = new byte[capacity];
            buffer1             = new byte[capacity];
            queue               = new List<MessageItem<TMeta>>();
        }
        
        public void Enqueue(in JsonValue value, in TMeta meta) {
            var array   = value.Array;
            var start   = value.start;
            var len     = value.Count;
            Enqueue(array, start, len, meta);
        }

        private void Enqueue(byte[] data, int start, int len, in TMeta meta) {
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
            var message = new JsonValue(buffer, bufferPos, len);
            queue.Add(new MessageItem<TMeta>(message, meta));
            SetBufferPos(bufferPos + len);
        }
        
        public MessageBufferEvent DequeMessages(List<MessageItem<TMeta>> messages)
        {
            messages.Clear();

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
        
        public void Clear() {
            SetBufferPos(0);
            queue.Clear();
        }
        
        public void Close() {
            closed = true;
        }
    }
}