﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Provide the entity id for each <see cref="Chunk{T}"/>.<see cref="Chunk{T}.Values"/> element with <see cref="Ids"/> or <see cref="IdAt"/>.<br/>
/// Its <see cref="Length"/> is equal to the <see cref="Chunk{T}"/>.<see cref="Chunk{T}.Values"/> Length.
/// </summary>
/// <remarks>
/// It implements <see cref="IEnumerable{T}"/> only to provide comprehensive information of <see cref="Entity"/>'s in a debugger.<br/>
/// Its unlikely to enumerate <see cref="ChunkEntities"/> in an application.<br/>
/// The recommended methods used by an application are <see cref="Ids"/>, <see cref="IdAt"/> or <see cref="EntityAt"/>.  
/// </remarks>
public readonly struct ChunkEntities : IEnumerable<Entity>
{
#region public properties
    public              ReadOnlySpan<int>   Ids         => new(Archetype.entityIds, idsStart, Length);
    public   override   string              ToString()  => GetString();
    #endregion

#region public / internal fields
    public   readonly   Archetype           Archetype;  //  8
    public   readonly   int                 Length;     //  4
    //
    internal readonly   int[]               entityIds;  //  8   - is redundant (archetype.entityIds) but avoid dereferencing for typical access pattern
    internal readonly   int                 idsStart;   //  4
    #endregion
    
    internal ChunkEntities(Archetype archetype, int chunkPos, int componentLen) {
        Archetype   = archetype;
        entityIds   = archetype.entityIds;
        Length      = componentLen;
        idsStart    = chunkPos * StructInfo.ChunkSize;
    }
    
#region public methods
    public int IdAt(int index) {
        if (index < Length) {
            return entityIds[idsStart + index];
        }
        throw new IndexOutOfRangeException();
    }
    
    public Entity EntityAt(int index) {
        if (index < Length) {
            return new Entity(entityIds[idsStart + index], Archetype.entityStore);
        }
        throw new IndexOutOfRangeException();
    }
    
    // --- IEnumerable<>
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => new ChunkEntitiesEnumerator(this);
    
    // --- IEnumerable
    IEnumerator                 IEnumerable.GetEnumerator() => new ChunkEntitiesEnumerator(this);
    
    // --- new
    public ChunkEntitiesEnumerator          GetEnumerator() => new ChunkEntitiesEnumerator(this);
    
    private string GetString() {
        var sb = new StringBuilder();
        sb.Append("Length: ");
        sb.Append(Length);
        sb.Append(",  Archetype: ");
        Archetype.GetString(sb);
        return sb.ToString();
    }

    #endregion
}

public struct ChunkEntitiesEnumerator : IEnumerator<Entity>
{
    private readonly    int[]           entityIds;      //  8
    private readonly    EntityStore     store;          //  8
    private readonly    int             last;           //  4
    private             int             index;          //  4
    
    internal ChunkEntitiesEnumerator(in ChunkEntities chunkEntities) {
        entityIds   = chunkEntities.entityIds;
        store       = chunkEntities.Archetype.entityStore;
        index       = chunkEntities.idsStart - 1; 
        last        = chunkEntities.Length + index;
    }
    
    // --- IEnumerator<>
    public readonly Entity Current   => new Entity(entityIds[index], store);
    
    // --- IEnumerator
    public bool MoveNext() {
        if (index < last) {
            index++;
            return true;
        }
        return false;  
    }

    [ExcludeFromCodeCoverage]
    public void Reset() {
        throw new NotImplementedException();
    }
    
    object IEnumerator.Current => Current;

    // --- IDisposable
    public void Dispose() { }
}