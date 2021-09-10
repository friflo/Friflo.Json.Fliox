// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.DB.Graph.Internal;
using Friflo.Json.Fliox.DB.Graph.Internal.KeyRef;
using Friflo.Json.Fliox.DB.Graph.Internal.Map;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.DB.Graph
{
    /// <summary>
    /// A <see cref="Ref{TKey,T}"/> is used to declare type safe fields being references to other entities in a data model.
    /// 
    /// <para>
    /// A reference is an <see cref="key"/> of type <see cref="TKey"/>. A reference can be in two states:
    ///   <para><b>unresolved</b>
    ///     Only the access to <see cref="key"/> is valid. This is always the case.
    ///     Access to the referenced entity instance via the property <see cref="Entity"/> result in an <see cref="Exception"/>.
    ///   </para> 
    ///   <para><b>resolved</b>
    ///     Access to the referenced entity instance is valid via the property <see cref="Entity"/>.
    ///   </para> 
    /// </para> 
    /// The <see cref="key"/> is used when serializing a <see cref="Ref{TKey,T}"/> field to and from JSON.  
    /// <para>
    ///     A <see cref="Ref{TKey,T}"/> can be assigned in three ways:
    ///     <para>1. By assigning an key of type <see cref="TKey"/>.                        Assigning a default (null) <see cref="TKey"/> is valid.</para>
    ///     <para>2. By assigning an entity.                                                Assigning null as entity is valid.</para>
    ///     <para>3. By assigning with another reference of type <see cref="Ref{TKey,T}"/>. Assigning a default <see cref="Ref{TKey,T}"/> is valid.</para>
    /// </para>
    /// 
    /// <para>
    ///     Access to <see cref="key"/> and property <see cref="Entity"/>:
    ///     <para>The <see cref="key"/> of a <see cref="Ref{TKey,T}"/> can be accessed at all time without any restrictions.</para>
    ///     <para>The property <see cref="Entity"/> enables access to the referenced entity instance.
    ///         If the <see cref="Ref{TKey,T}"/> was assigned by an entity the access has no restrictions.
    ///         If the <see cref="Ref{TKey,T}"/> was assigned by an key the referenced entity instance need to
    ///         be resolved upfront. For resolving see notes bellow.
    ///     </para>
    /// </para>
    /// <para>
    ///   To resolve the <see cref="Entity"/> by its <see cref="key"/> various options are available:
    ///   <para>By calling <see cref="FindBy"/> of a <see cref="Ref{TKey,T}"/> instance.</para>
    ///   <para>
    ///     When reading an entity instance containing a <see cref="Ref{TKey,T}"/> field
    ///     <see cref="EntitySet{TKey,T}.Read"/> returns a <see cref="ReadTask{TKey, T}"/> providing the possibility
    ///     to read referenced entity together with its parent by calling <see cref="ReadTask{TKey, T}.ReadRef{TKey,T}"/>.
    ///     <br></br>
    ///     Further more those tasks used to resolve references provide themself methods to resolve their references.
    ///     These are <see cref="ReadRefTask{TKey,T}"/> and <see cref="ReadRefsTask{TKey,T}"/>
    ///   </para>
    ///   In all these cases <see cref="Entity"/> is accessible after calling <see cref="EntityStore.Sync()"/>
    /// </para>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [Fri.TypeMapper(typeof(RefMatcher))]
    public struct Ref<TKey, T>  where T : class
    {
        // invariant of Ref<T> has following cases:
        //
        //      id == null,     entity == null      => Ref<> was assigned by an id or entity = null   
        //      id != null,     entity == null      => Ref<> was assigned by an id != null
        //      id != null,     entity != null      => Ref<> was assigned by an entity != null
        //
        //      set == null    =>  Ref<TKey,T> is not attached to a Peer<T> until now
        //      set != null    =>  Ref<TKey,T> is attached to a Peer<T>

                                    public   readonly   TKey        key;
        [DebuggerBrowsable(Never)]  private  readonly   T           entity;
        [DebuggerBrowsable(Never)]  private  readonly   bool        entityAssigned;
        [DebuggerBrowsable(Never)]  private             Peer<T>     peer;    // alternatively a EntitySetBase<T> could be used 

        public   override           string              ToString() => AsString();
        private                     string              AsString() => IsKeyNull() ? "null" : RefKeyMap.KeyToId(key).AsString();

        internal static readonly    RefKey<TKey, T>     RefKeyMap = RefKey.GetRefKey<TKey, T>();
        
        public Ref(TKey key) {
            this.key        = key;
            entity          = null;
            entityAssigned  = false;
            peer            = null;
        }
        
        public Ref(T entity) {
            TKey entityId = default;
            if (entity != null) {
                entityId = EntitySetBase<T>.EntityKeyMap.GetKeyAsType<TKey>(entity); // TAG_NULL_REF
            }
            key             = entityId;
            this.entity     = entity;
            entityAssigned  = true;
            peer            = null;
            if (entity != null && entityId == null)
                throw new ArgumentException($"constructing a Ref<>(entity != null) expect entity.key not null. Type: {typeof(T)}");
        }
        
        internal Ref(Peer<T> peer) {
            key             = RefKeyMap.IdToKey(peer.id);      // peer.id is never null
            entity          = null;
            entityAssigned  = false;
            this.peer       = peer;
        }

        public T        Entity {
            get {
                if (peer == null) {
                    if (key == null)    // RefKeyMap.IsKeyNull(key)
                        return null;
                    if (entityAssigned)
                        return entity;
                    throw new UnresolvedRefException("Accessed unresolved reference.", typeof(T), AsString());
                }
                if (peer.assigned)
                    return peer.NullableEntity;
                throw new UnresolvedRefException("Accessed unresolved reference.", typeof(T), AsString());
            }
        }

        public bool TryEntity(out T entity) {
            // same implementation as Entity
            if (peer == null) {
                if (key == null) {      // RefKeyMap.IsKeyNull(key)
                    entity = null;
                    return true;
                }
                entity = this.entity;
                return entityAssigned;
            }
            if (peer.assigned) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        }
        
        internal T                  GetEntity() { return entity; }
        internal Peer<T>            GetPeer()   { return peer; }
        
        /// <summary>
        /// Returns true only in case <see cref="TKey"/> is a reference type like string and the <see cref="key"/> is null.
        /// Return always false in case <see cref="TKey"/> is a value type like <see cref="int"/> or <see cref="Guid"/>
        /// as values type cannot be null. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKeyNull () {
            return RefKeyMap.IsKeyNull(key);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEqual (in Ref<TKey, T> other) {
            return EqualityComparer<TKey>.Default.Equals(key, other.key);
        }

        /// <summary>Performance note: Prefer using <see cref="IsEqual"/> as is compares without boxing</summary>
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<TKey, T> other = (Ref<TKey, T>)obj;
            return IsEqual(other);
        }

        public override int GetHashCode() {
            return key.GetHashCode();
        }

        public static implicit operator Ref<TKey, T>(T entity) {
            return new Ref<TKey, T> (entity);
        }
        
        /* public static implicit operator T(Ref<T> reference) {
            return reference.entity;
        } */

        public static implicit operator Ref<TKey, T>(TKey key) {
            return new Ref<TKey, T> (key);
        }

        public Find<TKey, T> FindBy(ReadTask<TKey, T> task) {
            // may validate that set is the same which created the PeerEntity<>
            var find    = task.Find(key);
            peer        = task.set.GetOrCreatePeerByKey(key, new JsonKey());
            return find;
        }
    }
}