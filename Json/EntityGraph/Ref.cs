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
    
    public readonly struct Ref<T>  where T : Entity
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
        internal readonly   PeerEntity<T>   peer;
        
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
    }
}