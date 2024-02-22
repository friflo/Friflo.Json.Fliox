﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable InlineTemporaryVariable
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// An entity list used to apply changes to all entities stored in the container.<br/>
/// Its recommended to reuse instances of this class to avoid unnecessary allocations.
/// </summary>
[DebuggerTypeProxy(typeof(EntityListDebugView))]
public sealed class EntityList : IEnumerable<Entity>
{
#region properties
    /// <summary> Returns the number of entities stored in the container. </summary>
    public              int         Count       => count;
    
    /// <summary> Return the ids of entities stored in the container. </summary>
    public ReadOnlySpan<int>        Ids         => new (ids, 0, count);
    
    public override     string      ToString()  => $"Count: {count}";
    #endregion
    
#region fields
    internal            int[]       ids;            //  8
    internal readonly   EntityStore entityStore;    //  8
    internal            int         count;          //  4
    #endregion
    
#region general
    /// <summary>
    /// Creates a container to store entities of the given <paramref name="store"/>.
    /// </summary>
    public EntityList(EntityStore store)
    {
        entityStore = store;
        ids         = new int[8];
    }
    
    /// <summary> Return the entity at the given <paramref name="index"/>.</summary>
    public Entity this[int index] => new Entity(entityStore, ids[index]);
    #endregion

#region add entities
    /// <summary> Removes all entities from the <see cref="EntityList"/>. </summary>
    public void Clear() {
        count = 0;
    }
    
    /// <summary>
    /// Adds the entity with the given <paramref name="id"/> to the end of the <see cref="EntityList"/>.
    /// </summary>
    public void AddEntity(int id) {
        if (ids.Length == count) {
            ArrayUtils.Resize(ref ids, 2 * count);
        }
        ids[count++] = id;
    }
    
    /// <summary>
    /// Adds the <paramref name="entity"/> and recursively all child entities of the given <paramref name="entity"/>
    /// to the end of the <see cref="EntityList"/>.
    /// </summary>
    public void AddEntityTree(Entity entity)
    {
        AddEntity(entity.Id);
        ref var node = ref entity.store.nodes[entity.Id];
        foreach (var child in new ChildEntities (entity.store, node.childIds, node.childCount))
        {
            AddEntityTree(child);
        }
    }
    #endregion
    
#region apply entity changes
    /// <summary>
    /// Adds the given <paramref name="tags"/> to all entities in the <see cref="EntityList"/>.
    /// </summary>
    public void ApplyAddTags(in Tags tags)
    {
        int index = 0;
        var store = entityStore;
        foreach (var id in Ids)
        {
            // don't capture store.nodes. Application event handler may resize
            ref var node = ref store.nodes[id]; 
            EntityStoreBase.AddTags(store, tags, id, ref node.archetype, ref node.compIndex, ref index);
        }
    }
    
    /// <summary>
    /// Removes the given <paramref name="tags"/> from all entities in the <see cref="EntityList"/>.
    /// </summary>
    public void ApplyRemoveTags(in Tags tags)
    {
        int index = 0;
        var store = entityStore;
        foreach (var id in Ids)
        {
            // don't capture store.nodes. Application event handler may resize
            ref var node = ref store.nodes[id];
            EntityStoreBase.RemoveTags(store, tags, id, ref node.archetype, ref node.compIndex, ref index);
        }
    }
    
    /// <summary>
    /// Apply the the given <paramref name="batch"/> to all entities in the <see cref="EntityList"/>. 
    /// </summary>
    public void ApplyBatch(EntityBatch batch)
    {
        var store = entityStore;
        foreach (var id in Ids) {
            store.ApplyBatchTo(batch, id);
        }
    }
    #endregion
    
#region enumerator

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="EntityList"/>. 
    /// </summary>
    public EntityListEnumerator             GetEnumerator() => new EntityListEnumerator (this);

    // --- IEnumerable
    IEnumerator                 IEnumerable.GetEnumerator() => new EntityListEnumerator (this);

    // --- IEnumerable<>
    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => new EntityListEnumerator (this);
    #endregion
}

/// <summary>
/// Enumerates the entities of an <see cref="EntityList"/>.
/// </summary>
public struct EntityListEnumerator : IEnumerator<Entity>
{
    private readonly    int[]       ids;        //  8
    private readonly    EntityStore store;      //  8
    private readonly    int         count;      //  4
    private             int         index;      //  4
    private             Entity      current;    // 16
    
    internal EntityListEnumerator(EntityList list) {
        ids     = list.ids;
        count   = list.count;
        store   = list.entityStore;
    }
    
    // --- IEnumerator
    public          void         Reset()    => index = 0;

    readonly object  IEnumerator.Current    => current;

    public   Entity              Current    => current;
    
    // --- IEnumerator
    public bool MoveNext()
    {
        if (index < count) {
            current = new Entity(store, ids[index++]);
            return true;
        }
        current = default;
        return false;
    }
    
    public readonly void Dispose() { }
}

internal sealed class EntityListDebugView
{
    [Browse(RootHidden)]
    internal            Entity[]    Entities => GetEntities();
    
    private readonly    EntityList  list;
    
    internal EntityListDebugView(EntityList list) {
        this.list = list;
    }
    
    private Entity[] GetEntities()
    {
        var ids     = list.ids;
        var store   = list.entityStore;
        var count   = list.count;
        var result  = new Entity[count];
        for (int n = 0; n < count; n++) {
            result[n] = new Entity(store, ids[n]);
        }
        return result;
    }
} 