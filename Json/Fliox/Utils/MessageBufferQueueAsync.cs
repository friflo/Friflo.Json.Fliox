// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Utils
{
    /// <summary>
    /// Asynchronous version of <see cref="MessageBufferQueue{T}"/> used to support
    /// awaiting new messages asynchronous with <see cref="DequeMessagesAsync"/> 
    /// </summary>
    public sealed class MessageBufferQueueAsync<TMeta> : IDisposable
    {
        private  readonly   MessageBufferQueue<TMeta>   queue;
        private  readonly   SemaphoreSlim               messageAvailable;

        public              bool                        Closed { get { lock (queue) { return queue.Closed; } }}
        public   override   string                      ToString()  => GetString();

        public MessageBufferQueueAsync(int capacity = 4) {
            queue               = new MessageBufferQueue<TMeta>(capacity);
            messageAvailable    = new SemaphoreSlim(0, 1);
        }
        
        public void Dispose() {
            messageAvailable.Dispose();
        }
        
        public void AddTail(in JsonValue data, in TMeta meta = default) {
            lock (queue) {
                queue.AddTail(data, meta);
            }
            // send event _after_ adding message to queue
            if (messageAvailable.CurrentCount == 0) {
                messageAvailable.Release();
            }
        }
        
        public async Task<MessageBufferEvent> DequeMessageValuesAsync(List<JsonValue> messages) {
            messages.Clear();
            await messageAvailable.WaitAsync().ConfigureAwait(false);

            lock (queue) {
                return queue.DequeMessageValues(messages);    
            }
        }
        
        public async Task<MessageBufferEvent> DequeMessagesAsync(List<MessageItem<TMeta>> messages) {
            messages.Clear();
            await messageAvailable.WaitAsync().ConfigureAwait(false);

            lock (queue) {
                return queue.DequeMessages(messages);    
            }
        }

        public void Close() {
            lock (queue) {
                queue.Close();
                if (messageAvailable.CurrentCount == 0) {
                    messageAvailable.Release();
                }
            }
        }
        
        private string GetString() {
            lock (queue) {
                return $"Count = {queue.Count}";
            }
        }
    }
}