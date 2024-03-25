// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Friflo.Json.Fliox.Utils
{
    /// <summary>
    /// Synchronous version of <see cref="MessageBufferQueue{T}"/> used to support
    /// waiting for new messages synchronous with <see cref="DequeMessages"/> 
    /// </summary>
    public sealed class MessageBufferQueueSync<TMeta> : IDisposable
    {
        private  readonly   MessageBufferQueue<TMeta>   queue;
        private  readonly   ManualResetEvent            messageAvailable;

        public              bool                        Closed { get { lock (queue) { return queue.Closed; } }}
        public   override   string                      ToString() => GetString();

        public MessageBufferQueueSync(int capacity = 4) {
            queue               = new MessageBufferQueue<TMeta>(capacity);
            messageAvailable    = new ManualResetEvent(false);
        }
        
        public void Dispose() {
            messageAvailable.Dispose();
        }
        
        public void AddTail(in JsonValue data, in TMeta meta = default) {
            lock (queue) {
                queue.AddTail(data, meta);
            }
            messageAvailable.Set();
        }
        
        public MessageBufferEvent DequeMessageValues(List<JsonValue> messages) {
            messages.Clear();
            messageAvailable.WaitOne();
            messageAvailable.Reset();

            lock (queue) {
                return queue.DequeMessageValues(messages);    
            }
        }
        
        public MessageBufferEvent DequeMessages(List<MessageItem<TMeta>> messages) {
            messages.Clear();
            messageAvailable.WaitOne();
            messageAvailable.Reset();

            lock (queue) {
                return queue.DequeMessages(messages);    
            }
        }

        public void Close() {
            lock (queue) {
                queue.Close();
            }
            messageAvailable.Set();
        }
        
        private string GetString() {
            lock (queue) {
                return $"Count = {queue.Count}";
            }
        }
    }
}