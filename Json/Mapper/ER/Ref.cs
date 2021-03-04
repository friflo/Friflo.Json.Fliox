namespace Friflo.Json.Mapper.ER
{
    // Change to attribute
    public class Entity {
        public string   id;
    }
    
    public class Ref<T>  where T : Entity
    {
        private  T        entity;
        private  string   id;
        
        // either id or entity is set. Never both
        public string   Id {
            get => entity != null ? entity.id : id;
            set { id = value; entity = null; }
        }

        public T        Entity {
            get => entity;
            set { entity = value; id = null; }
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<T> other = (Ref<T>)obj;
            return Id.Equals(other.Id);
        }

        public static implicit operator Ref<T>(T entity) {
            var reference = new Ref<T>();
            reference.entity    = entity;
            return reference;
        }
        
        /* public static implicit operator T(Ref<T> reference) {
            return reference.entity;
        } */


        public static implicit operator Ref<T>(string id) {
            var reference = new Ref<T>();
            reference.id    = id;
            return reference;
        }
    }
}