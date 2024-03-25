// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// Different implementation proposals can be found via google: async task queue c#
// e.g.
// [c# - awaitable Task based queue - Stack Overflow] https://stackoverflow.com/questions/7863573/awaitable-task-based-queue

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Threading
{
    /// Added <see cref="DataChannelSlim{T}"/> as a stub to enable compiling in Unity as there are no <see cref="System.Threading.Channels.Channel"/>'s
    /// available as of 2021-06-21.
    internal sealed class DataChannelSlim<T> : IDataChannel<T>
    {
        public   IDataChannelReader<T>   Reader { get; } 
        public   IDataChannelWriter<T>   Writer { get; }
        
        internal readonly SemaphoreSlim          itemsAvailable = new SemaphoreSlim(0);
        internal readonly ConcurrentQueue<T>     queue          = new ConcurrentQueue<T>();

        private DataChannelSlim() {
            Reader = new DataChannelReaderSlim<T>(this);
            Writer = new DataChannelWriterSlim<T>(this);
        }

        public void Dispose() {
            itemsAvailable.Dispose();
        }

        public static DataChannelSlim<T> CreateUnbounded(bool singleReader, bool singleWriter) {
            return new DataChannelSlim<T>();
        }
    }
    
    internal sealed class DataChannelReaderSlim<T> : IDataChannelReader<T> {
        private readonly DataChannelSlim<T> channel;
        
        internal DataChannelReaderSlim(DataChannelSlim<T> channel) {
            this.channel = channel;
        }
        
        public async Task<T> ReadAsync (CancellationToken cancellationToken = default) {
            await channel.itemsAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);

            channel.queue.TryDequeue(out T item);
            return item;
        }
    }
    
    internal sealed class DataChannelWriterSlim<T> : IDataChannelWriter<T> {
        private readonly DataChannelSlim<T> channel;
        
        internal DataChannelWriterSlim(DataChannelSlim<T> channel) {
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

