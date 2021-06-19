// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// Different implementation proposals can be found via google: async task queue c#
// e.g.
// [c# - awaitable Task based queue - Stack Overflow] https://stackoverflow.com/questions/7863573/awaitable-task-based-queue

#if UNITY_5_3_OR_NEWER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Utils
{
    /// Added <see cref="DataChannel{T}"/> as a stub to enable compiling in Unity as there are no <see cref="Channel"/>'s
    /// available as of 2021-06-21.
    public class DataChannel<T>
    {
        public  readonly DataChannelReader<T>   reader; 
        public  readonly DataChannelWriter<T>   writer; 
     // private readonly Channel<T>             channel;

        private DataChannel() {
            reader = new DataChannelReader<T>();
            writer = new DataChannelWriter<T>();
        }
        
        public static DataChannel<T> CreateUnbounded(bool singleReader, bool singleWriter) {
            throw new NotImplementedException("in Unity");
        }
    }
    
    public class DataChannelReader<T> {
        // private readonly ChannelReader<T> reader;
        
        internal DataChannelReader() {
        }
        
        public Task<T> ReadAsync (CancellationToken cancellationToken = default) {
            throw new NotImplementedException("in Unity");
        }
    }
    
    public class DataChannelWriter<T> {
        // private readonly ChannelWriter<T> writer;
        
        internal DataChannelWriter() {
        }
        
        public bool TryWrite(T data) {
            throw new NotImplementedException("in Unity");
        }
        
        public void Complete(Exception error = null) {
            throw new NotImplementedException("in Unity");
        }
    }
}

#endif