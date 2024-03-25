// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Fliox.Utils
{
    public enum MessageBufferEvent {
        Closed      = 1,
        NewMessage  = 2,
    }
    
    public readonly struct MessageItem<T>
    {
        public  readonly    JsonValue   value;
        public  readonly    T           meta;

        public  override    string      ToString() => GetString();
        
        private string GetString() {
            var metaStr = meta.ToString();
            if (metaStr?.Length > 0) {
                var sb = new StringBuilder();
                sb.Append(metaStr);
                sb.Append("  value: ");
                sb.Append(value.AsString());
                return sb.ToString();
            }
            return value.AsString();
        }

        internal MessageItem (in JsonValue value, T meta) {
            this.value  = value;
            this.meta   = meta;
        }
    }
    
    public readonly struct VoidMeta {
        public override string ToString() => "";
    }

    /// <summary>
    /// A queue used to store <see cref="JsonValue"/> messages using double buffering to avoid frequent allocations
    /// of byte[] instances for each message.
    /// </summary>
    /// <remarks>
    /// To avoid frequent byte[] allocations it utilizes two byte[] buffers.<br/>
    /// 1. One buffer is used to store the bytes of new messages. <br/>
    /// 2. The other buffer store the bytes of dequeued messages. <br/>
    /// <b>Note</b>
    /// Dequeued messages are valid until the next call of <see cref="DequeMessageValues"/><br/>
    /// The buffers are swapped when calling <see cref="DequeMessageValues"/>.<br/>
    /// <b>Note</b>
    /// <see cref="MessageBufferQueue{TMeta}"/> is not thread safe<br/>
    /// <br/>
    /// </remarks>
    /// <typeparam name="TMeta">
    /// Type of the meta data associated to each message.
    /// Use <see cref="VoidMeta"/> if no associated meta data is required
    /// </typeparam>
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
        
        public              int             Count           => deque.Count;
        public              bool            Closed          => closed;

        private  readonly   Deque<MessageItem<TMeta>> deque;
        
        private             bool            closed;

        public   override   string          ToString()      => $"Count = {deque.Count}";
        

        public MessageBufferQueue(int capacity = 4) {
            deque   = new Deque<MessageItem<TMeta>>(capacity);
        }
        
        public MessageItem<TMeta> GetHead() {
            if (Count == 0) throw new InvalidOperationException("MessageBufferQueue is empty");
            return deque.Array[deque.First];
        }
        
        public MessageItem<TMeta> RemoveHead() {
            if (closed) throw new InvalidOperationException("MessageBufferQueue already closed");
            return deque.RemoveHead();
        }

        /// <summary>Add add copy of the given <paramref name="value"/> to the head of the queue</summary>
        public void AddHead(in JsonValue value, in TMeta meta = default) {
            if (closed) throw new InvalidOperationException("MessageBufferQueue already closed");
            var message = CreateMessageValue(value);
            deque.AddHead(new MessageItem<TMeta>(message, meta));
        }
        
        /// <summary>Add add copy of the given <paramref name="value"/> to the tail of the queue</summary>
        public void AddTail(in JsonValue value, in TMeta meta = default) {
            if (closed) throw new InvalidOperationException("MessageBufferQueue already closed");
            var message = CreateMessageValue(value);
            deque.AddTail(new MessageItem<TMeta>(message, meta));
        }
        
        private JsonValue CreateMessageValue(in JsonValue value) {
            int len             = value.Count;
            var buffer          = Buffer;
            var bufferLen       = buffer?.Length ?? 0;
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
            System.Buffer.BlockCopy(value.Array, value.start, buffer, bufferPos, len);
            SetBufferPos(bufferPos + len);
            return new JsonValue(buffer, bufferPos, len);
        }
        
        /// <summary>
        /// Dequeue all queued message value<br/> 
        /// The returned <paramref name="messages"/> are valid until the next <see cref="DequeMessageValues"/> call.<br/>
        /// Similar to <see cref="DequeMessages"/> but return only <see cref="JsonValue"/>'s
        /// </summary>
        public MessageBufferEvent DequeMessageValues(List<JsonValue> messages)
        {
            messages.Clear();

            // swap read & write buffer.
            writeBuffer = writeBuffer == 0 ? 1 : 0;
            // newly enqueued messages are written to the head of the write buffer
            SetBufferPos(0);
            foreach (var message in deque) {
                messages.Add(message.value);                    
            }
            deque.Clear();
            return closed ? MessageBufferEvent.Closed : MessageBufferEvent.NewMessage;
        }
        
        /// <summary>
        /// Dequeue all queued messages <br/>
        /// The returned <paramref name="messages"/> are valid until the next <see cref="DequeMessages"/> call.<br/>
        /// Similar to <see cref="DequeMessageValues"/> but return <see cref="JsonValue"/>'s and associated meta data.
        /// </summary>
        public MessageBufferEvent DequeMessages(List<MessageItem<TMeta>> messages)
        {
            messages.Clear();

            // swap read & write buffer.
            writeBuffer = writeBuffer == 0 ? 1 : 0;
            // newly enqueued messages are written to the head of the write buffer
            SetBufferPos(0);
            foreach (var message in deque) {
                messages.Add(message);                    
            }
            deque.Clear();
            return closed ? MessageBufferEvent.Closed : MessageBufferEvent.NewMessage;
        }
        
        public void Clear() {
            // newly enqueued messages are written to the head of the write buffer
            SetBufferPos(0);
            deque.Clear();
        }
        
        public void Close() {
            closed = true;
        }
        
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        
        // ---------------------------------------- Enumerator<T> ----------------------------------------
        public struct Enumerator
        {
            private readonly    MessageItem<TMeta>[]    items;
            private readonly    int                     capacity;
            private             int                     remaining;
            private             int                     next;
            private             int                     current;
            
            internal Enumerator (MessageBufferQueue<TMeta> queue) {
                var deque   = queue.deque;
                items       = deque.Array;
                capacity    = deque.Capacity;
                remaining   = deque.Count;
                next        = deque.First;
                current     = -1;
            }
            
            public bool MoveNext() {
                if (remaining > 0) {
                    current = next;
                    next    = (next + 1) % capacity;
                    remaining--;
                    return true;
                }
                current = -1;
                return false;
            }
        
            public MessageItem<TMeta> Current => current != -1 ? items[current] : throw new InvalidOperationException("invalid enumerator");
        }
    }
}