// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// Different implementation proposals can be found via google: async task queue c#
// e.g.
// [c# - awaitable Task based queue - Stack Overflow] https://stackoverflow.com/questions/7863573/awaitable-task-based-queue

#if UNITY_5_3_OR_NEWER

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

// Note!!    Implementation untested
namespace Friflo.Json.Fliox.DB.Host.Utils
{
    /// Added <see cref="DataChannel{T}"/> as a stub to enable compiling in Unity as there are no <see cref="Channel"/>'s
    /// available as of 2021-06-21.
    public sealed class DataChannel<T>
    {
        public   readonly DataChannelReader<T>   reader; 
        public   readonly DataChannelWriter<T>   writer;
        
        internal readonly SemaphoreSlim          itemsAvailable = new SemaphoreSlim(0);
        internal readonly ConcurrentQueue<T>     queue          = new ConcurrentQueue<T>();

        private DataChannel() {
            reader = new DataChannelReader<T>(this);
            writer = new DataChannelWriter<T>(this);
        }
        
        public static DataChannel<T> CreateUnbounded(bool singleReader, bool singleWriter) {
            return new DataChannel<T>();
        }
    }
    
    public class DataChannelReader<T> {
        private readonly DataChannel<T> channel;
        
        internal DataChannelReader(DataChannel<T> channel) {
            this.channel = channel;
        }
        
        public async Task<T> ReadAsync (CancellationToken cancellationToken = default) {
            await channel.itemsAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);

            channel.queue.TryDequeue(out T item);
            return item;
        }
    }
    
    public class DataChannelWriter<T> {
        private readonly DataChannel<T> channel;
        
        internal DataChannelWriter(DataChannel<T> channel) {
            this.channel = channel;
        }
        
        public bool TryWrite(T data) {
            channel.queue.Enqueue(data);
            channel.itemsAvailable.Release();
            return true;
        }
        
        public void Complete(Exception error = null) {
            // Do nothing for now. Assumption: completing queue is not a hard requirement
        }
    }
}

#endif