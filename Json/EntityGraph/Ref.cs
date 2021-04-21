// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    // Change to attribute
    public class Entity {
        public              string  id;

        public override     string  ToString() => id;
    }
    
    public readonly struct Ref<T>  where T : Entity
    {
        internal readonly   PeerEntity<T>   peer;
        private  readonly   T               entity;     // not null, if set by application
        private  readonly   string          id;         // not null, if set by application
        
        public   override   string          ToString() => Id;
        
        // public Ref() { }

        public Ref(string id) {
            this.id     = id;
            peer        = null;
            entity      = null;
        }
        
        public Ref(T entity) {
            this.entity = entity;
            peer        = null;
            id          = null;
        }
        
        internal Ref(PeerEntity<T> peer) {
            this.peer   = peer;
            this.entity = peer.entity;
            this.id     = null;
        }

        // either id or entity is set. Never both
        public string   Id => entity != null ? entity.id : id;

        public T        Entity {
            get {
                if (entity != null)
                    return entity;
                if (peer != null) {
                    if (peer.assigned)
                        return peer.entity;
                    throw new PeerNotAssignedException(peer.entity);
                }
                return null;
            }
        }
        
        internal T GetEntity() {
            return entity;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<T> other = (Ref<T>)obj;
            return Id.Equals(other.Id);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
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