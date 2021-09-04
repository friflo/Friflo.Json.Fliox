// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.DB.Database.Utils
{
    /// Added <see cref="DataChannel{T}"/> as a stub to enable compiling in Unity as there are no <see cref="Channel"/>'s
    /// available as of 2021-06-21.
    public class DataChannel<T>
    {
        public  readonly    DataChannelReader<T>    reader; 
        public  readonly    DataChannelWriter<T>    writer; 
     // private readonly Channel<T>                 channel;

        private DataChannel(Channel<T, T> channel) {
            reader = new DataChannelReader<T>(channel.Reader);
            writer = new DataChannelWriter<T>(channel.Writer);
        }
        
        public static DataChannel<T> CreateUnbounded(bool singleReader, bool singleWriter) {
            var opt = new UnboundedChannelOptions {
                SingleReader = singleReader,
                SingleWriter = singleWriter
            };
            // opt.AllowSynchronousContinuations = true;
            var channel = Channel.CreateUnbounded<T>(opt);
            return new DataChannel<T>(channel);
        }
    }
    
    public class DataChannelReader<T> {
        private readonly ChannelReader<T> reader;
        
        internal DataChannelReader(ChannelReader<T> reader) {
            this.reader = reader;
        }
        
        public async Task<T> ReadAsync (CancellationToken cancellationToken = default) {
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    
    public class DataChannelWriter<T> {
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