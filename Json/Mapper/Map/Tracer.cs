using Friflo.Json.Mapper.ER;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Tracer
    {
        public readonly     TypeCache           typeCache;
        public readonly     EntityStore         entityStore;

        public Tracer(TypeCache typeCache, EntityStore entityStore) {
            this.typeCache = typeCache;
            this.entityStore = entityStore;
        }
        
        public void Trace<T>(T value) {
            var mapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            if (!mapper.IsNull(ref value))
                mapper.Trace(this, value);
        }
    }
}