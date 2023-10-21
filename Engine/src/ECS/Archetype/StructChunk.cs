﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

/// <remarks>
/// <b>Note!</b> Must not contain any other field. Reasons:<br/>
/// - to save memory as many <see cref="StructChunk{T}"/>'s are stored within a <see cref="StructHeap{T}.chunks"/><br/>
/// - to enable maximum efficiency when GC iterate <see cref="StructHeap{T}.chunks"/> for collection.
/// </remarks>
internal readonly struct StructChunk<T>
    where T : struct, IStructComponent
{
    // Note! Must not contain any other field. See <remarks>
    internal readonly   T[]     components;   // 8
    
    public   override   string  ToString() => components == null ? "" : "used";
    
    internal StructChunk (int chunkSize) {
        components  = new T[chunkSize];
    }
}

public readonly struct Chunk<T>
    where T : struct, IStructComponent
{
    public              Span<T> Values => new(values, 0, count);

    private readonly    T[]     values;
    private readonly    int     count;
    
    public Chunk(T[] values, int count) {
        this.values = values;
        this.count  = count;
    }
}
