// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Threading
{
    [ExcludeFromCodeCoverage]
    internal sealed class DataChannel<T> : IDataChannel<T>
    {
        public   IDataChannelReader<T>   Reader { get; } 
        public   IDataChannelWriter<T>   Writer { get; }
     // private readonly Channel<T>                 channel;

        private DataChannel(Channel<T, T> channel) {
            Reader = new DataChannelReader<T>(channel.Reader);
            Writer = new DataChannelWriter<T>(channel.Writer);
        }

        public void Dispose() { }

        internal static DataChannel<T> CreateUnbounded(bool singleReader, bool singleWriter) {
            var opt = new UnboundedChannelOptions {
                SingleReader = singleReader,
                SingleWriter = singleWriter
            };
            // opt.AllowSynchronousContinuations = true;
            var channel = Channel.CreateUnbounded<T>(opt);
            return new DataChannel<T>(channel);
        }
    }
    
    [ExcludeFromCodeCoverage]
    internal sealed class DataChannelReader<T> : IDataChannelReader<T> {
        private readonly ChannelReader<T> reader;
        
        internal DataChannelReader(ChannelReader<T> reader) {
            this.reader = reader;
        }
        
        public async Task<T> ReadAsync (CancellationToken cancellationToken = default) {
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    
    [ExcludeFromCodeCoverage]
    internal sealed class DataChannelWriter<T> : IDataChannelWriter<T> {
        private readonly ChannelWriter<T> writer;
        
        internal DataChannelWriter(ChannelWriter<T> writer) {
            this.writer = writer;
        }
        
        public bool TryWrite(T data) {
            return writer.TryWrite(data);
        }
        
        public void Complete(Exception error = null) {
            writer.Complete(error);
        }
    }
}

#endif