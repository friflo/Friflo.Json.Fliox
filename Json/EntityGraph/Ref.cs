// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.EntityGraph.Internal;
// ReSharper disable InconsistentNaming

namespace Friflo.Json.EntityGraph
{
    // Change to attribute
    public class Entity
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _id;
        
        public string id {
            get => _id;
            set {
                if (_id == value)
                    return;
                if (_id == null) {
                    _id = value;
                    return;
                }
                throw new InvalidOperationException($"Entity id must not be changed. Type: {GetType()}, id: {_id}, used: {value}");
            }
        }

        public override     string  ToString() => id ?? "null";
    }
    
    /// <summary>
    /// A <see cref="Ref{T}"/> is used to model a reference to an <see cref="EntityGraph.Entity"/> in a data modal
    /// by id of type <see cref="string"/>.
    /// The <see cref="id"/> is used when serializing a <see cref="Ref{T}"/> field to and from JSON.  
    /// <para>
    /// A <see cref="Ref{T}"/> can be assigned in three ways:
    ///     <para>1. By assigning an id of type <see cref="string"/>. Assigning a null <see cref="string"/> is valid.</para>
    ///     <para>2. By assigning an entity of a type extending <see cref="EntityGraph.Entity"/>. Assigning a null <see cref="EntityGraph.Entity"/> is valid.</para>
    ///     <para>3. By assigning with another reference of type <see cref="Ref{T}"/>. Assigning a default <see cref="Ref{T}"/> is valid.</para>
    /// </para>
    /// 
    /// <para>
    ///     Access to <see cref="id"/> and <see cref="Entity"/>:
    ///     <para>The <see cref="id"/> of a <see cref="Ref{T}"/> can be accessed at all time without any restrictions.</para>
    ///     <para><see cref="Entity"/> enables access to the referenced <see cref="EntityGraph.Entity"/> instance.
    ///         If the <see cref="Ref{T}"/> was assigned by an entity the access has no restrictions.
    ///         If the <see cref="Ref{T}"/> was assigned by an id the referenced <see cref="EntityGraph.Entity"/> need to
    ///         be resolved upfront. For resolving see notes bellow.
    ///     </para>
    /// </para>
    /// <para>
    ///     To resolve the <see cref="Entity"/> by its <see cref="id"/> various options are available:
    ///     <para>By calling <see cref="Read"/> of a <see cref="Ref{T}"/> instance.</para>
    ///     <para>
    ///         When reading the parent <see cref="EntityGraph.Entity"/> containing a <see cref="Ref{T}"/> field
    ///         <see cref="EntitySet{T}.Read"/> returns a <see cref="ReadTask{T}"/> providing the possibility
    ///         to read referenced entity by calling <see cref="ReadTask{T}.ReadRef{TValue}"/>.  
    ///     </para>
    ///     In all these cases <see cref="Entity"/> is accessible after calling <see cref="EntityStore.Sync()"/>
    /// </para>
    /// </summary>
    public struct Ref<T>  where T : Entity
    {
        // invariant of Ref<T> has following cases:
        //
        //      id == null,     entity == null,     peer == null
        //      id != null,     entity == null,     peer == null
        //      id != null,     entity != null,     peer == null
        //      id != null,     entity != null,     peer != null
        //
        //      peer == null    =>  application  assigned id & entity to Ref<T>
        //      peer != null    =>  EntitySet<T> assigned id & entity to Ref<> via Read(), ReadRef() or Query()

        public   readonly   string          id;
        private  readonly   T               entity;
        internal            PeerEntity<T>   peer;
        
        public   override   string          ToString() => id ?? "null";
        
        public Ref(string id) {
            this.id     = id;
            this.entity = null;
            this.peer   = null;
        }
        
        public Ref(T entity) {
            this.id     = entity?.id;
            this.entity = entity;
            this.peer   = null;
            if (entity != null && entity.id == null)
                throw new InvalidOperationException($"constructing a Ref<>(entity != null) expect entity.id not null. Type: {typeof(T)}");
        }
        
        internal Ref(PeerEntity<T> peer) {
            this.id     = peer.entity.id;  // peer.entity never null
            this.entity = peer.entity;
            this.peer   = peer;
        }

        public T        Entity {
            get {
                if (peer == null)
                    return entity;
                if (peer.assigned)
                    return peer.entity;
                throw new PeerNotAssignedException(peer.entity);
            }
        }
        
        internal T GetEntity() {
            return entity;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<T> other = (Ref<T>)obj;
            return id.Equals(other.id);
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }

        public static implicit operator Ref<T>(T entity) {
            return new Ref<T> (entity);
        }
        
        /* public static implicit operator T(Ref<T> reference) {
            return reference.entity;
        } */

        public static implicit operator Ref<T>(string id) {
            return new Ref<T> (id);
        }

        public ReadTask<T> Read(EntitySet<T> set) {
            // may validate that set is the same which created the PeerEntity<>
            
            var readTask = set.Read(id);
            peer = readTask.peer;
            return readTask;
        }
    }
}