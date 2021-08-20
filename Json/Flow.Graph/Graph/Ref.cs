// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Graph.Internal.Id;

namespace Friflo.Json.Flow.Graph
{
    /// <summary>
    /// A <see cref="Ref{T,K}"/> is used to declare type safe fields being references to other entities in a data model.
    /// 
    /// <para>
    /// A reference is an <see cref="key"/> of type <see cref="string"/>. A reference can be in two states:
    ///   <para><b>unresolved</b>
    ///     Only the access to <see cref="key"/> is valid. This is always the case.
    ///     Access to the referenced entity instance via the property <see cref="Entity"/> result in an <see cref="Exception"/>.
    ///   </para> 
    ///   <para><b>resolved</b>
    ///     Access to the referenced entity instance is valid via the property <see cref="Entity"/>.
    ///   </para> 
    /// </para> 
    /// The <see cref="key"/> is used when serializing a <see cref="Ref{T}"/> field to and from JSON.  
    /// <para>
    ///     A <see cref="Ref{T}"/> can be assigned in three ways:
    ///     <para>1. By assigning an id of type <see cref="string"/>.                   Assigning a null <see cref="string"/> is valid.</para>
    ///     <para>2. By assigning an entity.                                            Assigning null as entity is valid.</para>
    ///     <para>3. By assigning with another reference of type <see cref="Ref{T}"/>.  Assigning a default <see cref="Ref{T}"/> is valid.</para>
    /// </para>
    /// 
    /// <para>
    ///     Access to <see cref="key"/> and property <see cref="Entity"/>:
    ///     <para>The <see cref="key"/> of a <see cref="Ref{T}"/> can be accessed at all time without any restrictions.</para>
    ///     <para>The property <see cref="Entity"/> enables access to the referenced entity instance.
    ///         If the <see cref="Ref{T}"/> was assigned by an entity the access has no restrictions.
    ///         If the <see cref="Ref{T}"/> was assigned by an id the referenced entity instance need to
    ///         be resolved upfront. For resolving see notes bellow.
    ///     </para>
    /// </para>
    /// <para>
    ///   To resolve the <see cref="Entity"/> by its <see cref="key"/> various options are available:
    ///   <para>By calling <see cref="FindBy"/> of a <see cref="Ref{T}"/> instance.</para>
    ///   <para>
    ///     When reading an entity instance containing a <see cref="Ref{T}"/> field
    ///     <see cref="EntitySet{T}.Read"/> returns a <see cref="ReadTask{T}"/> providing the possibility
    ///     to read referenced entity together with its parent by calling <see cref="ReadTask{T}.ReadRef{TValue}"/>.
    ///     <br></br>
    ///     Further more those tasks used to resolve references provide themself methods to resolve their references.
    ///     These are <see cref="ReadRefTask{T}"/> and <see cref="ReadRefsTask{T}"/>
    ///   </para>
    ///   In all these cases <see cref="Entity"/> is accessible after calling <see cref="EntityStore.Sync()"/>
    /// </para>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public struct Ref<TKey, T>  where T : class
    {
        // invariant of Ref<T> has following cases:
        //
        //      id == null,     entity == null      => Ref<> was assigned by an id or entity = null   
        //      id != null,     entity == null      => Ref<> was assigned by an id != null
        //      id != null,     entity != null      => Ref<> was assigned by an entity != null
        //
        //      peer == null    =>  Ref<> is not attached to a peer until now
        //      peer != null    =>  Ref<> is attached to a peer

        public   readonly   TKey            key;
        public   readonly   string          id;
        private  readonly   T               entity;
        private             PeerEntity<T>   peer;
        
        internal static readonly   EntityId<TKey, T>     StaticEntityId = EntityId.GetEntityId2<TKey, T>();
        
        public   override   string          ToString() => id ?? "null";
        
        public Ref(TKey key) {
            this.key    = key;
            id          = StaticEntityId.KeyToId(key);
            this.entity = null;
            this.peer   = null;
        }
        
        public Ref(T entity) {
            TKey entityId = entity != null ? StaticEntityId.GetKey(entity) : default;
            this.key    = entityId;
            this.id     = StaticEntityId.KeyToId(key);
            this.entity = entity;
            this.peer   = null;
            if (entity != null && entityId == null)
                throw new ArgumentException($"constructing a Ref<>(entity != null) expect entity.id not null. Type: {typeof(T)}");
        }
        
        internal Ref(PeerEntity<T> peer) {
            this.key    = StaticEntityId.IdToKey(peer.id);      // peer.id is never null
            this.id     = peer.id;
            this.entity = null;
            this.peer   = peer;
        }

        public T        Entity {
            get {
                if (peer == null)
                    return entity;
                if (peer.assigned)
                    return peer.Entity;
                throw new UnresolvedRefException("Accessed unresolved reference.", typeof(T), id);
            }
        }

        public bool TryEntity(out T entity) {
            // same implementation as Entity
            if (peer == null) {
                entity = this.entity;
                return true;
            }
            if (peer.assigned) {
                entity = peer.Entity;
                return true;
            }
            entity = null;
            return false;
        }
        
        internal T              GetEntity() { return entity; }
        internal PeerEntity<T>  GetPeer()   { return peer; }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<TKey, T> other = (Ref<TKey, T>)obj;
            return key.Equals(other.key);
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

        public static implicit operator Ref<TKey, T>(TKey id) {
            return new Ref<TKey, T> (id);
        }

        public Find<T> FindBy(ReadTask<TKey, T> task) {
            // may validate that set is the same which created the PeerEntity<>
            var find = task.Find(id);
            peer = task.set.GetPeerById(id);
            return find;
        }
    }
}