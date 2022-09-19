// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Threading
{
    public interface IDataChannel<T> : IDisposable
    {
        IDataChannelReader<T>   Reader { get; } 
        IDataChannelWriter<T>   Writer { get; }
    }
    
    public interface IDataChannelReader<T>
    {
        Task<T> ReadAsync (CancellationToken cancellationToken = default);
    }
    
    public interface IDataChannelWriter<in T>
    {
        bool TryWrite(T data);
        void Complete(Exception error = null);
    } 
}