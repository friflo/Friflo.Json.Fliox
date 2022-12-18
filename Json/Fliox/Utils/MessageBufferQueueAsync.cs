// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Utils
{
    /// <summary>
    /// Asynchronous version of <see cref="MessageBufferQueue"/> used to support
    /// awaiting new messages asynchronous with <see cref="DequeMessagesAsync"/> 
    /// </summary>
    public class MessageBufferQueueAsync : IDisposable
    {
        private  readonly   MessageBufferQueue  queue;
        private  readonly   SemaphoreSlim       messageAvailable;
        
        public MessageBufferQueueAsync(int capacity = 128) {
            queue               = new MessageBufferQueue(capacity);
            messageAvailable    = new SemaphoreSlim(0, 1);
        }
        
        public void Dispose() {
            messageAvailable.Dispose();
        }
        
        public void Enqueue(in JsonValue data) {
            lock (queue) {
                queue.Enqueue(data);
            }
            // send event _after_ adding message to queue
            if (messageAvailable.CurrentCount == 0) {
                messageAvailable.Release();
            }
        }
        
        public async Task<MessageBufferEvent> DequeMessagesAsync(List<JsonValue> messages) {
            await messageAvailable.WaitAsync().ConfigureAwait(false);

            lock (queue) {
                return queue.DequeMessages(messages);    
            }
        }

        public void Close() {
            lock (queue) {
                queue.Close();
            }
            if (messageAvailable.CurrentCount == 0) {
                messageAvailable.Release();
            }
        }
    }
}