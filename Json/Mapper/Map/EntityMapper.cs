using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    // -------------------------------------------------------------------------------------
    
    public class EntityMatcher : ITypeMatcher {
        public static readonly EntityMatcher Instance = new EntityMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            if (!typeof(Entity).IsAssignableFrom(type))
                return null;
            
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new EntityMapper<T>(config, type, constructor, mapper);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(EntityMapper<>), new[] {type}, constructorParams);
        }
    }


    public class EntityMapper<T> : TypeMapper<T> where T : Entity
    {
        private readonly TypeMapper<T> mapper;
        
        public override string DataTypeName() { return "Entity"; }

        public EntityMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, true, true)
        {
            mapper = (TypeMapper<T>)ClassMatcher.Instance.MatchTypeMapper(type, config);
        }
        
        public override void Dispose() {
            base.Dispose();
            mapper.Dispose();
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            mapper.InitTypeMapper(typeStore);
        }
        
        public override void Write(ref Writer writer, T value) {
            if (writer.database != null && writer.Level > 0) {
                if (value != null) {
                    writer.WriteString(value.id);
                    var container = writer.database.GetContainer(typeof(T));
                    container.AddEntity(value);
                } else {
                    writer.AppendNull();
                }
            } else {
                if (value != null) {
                    mapper.WriteObject(ref writer, value);
                    if (writer.database != null) {
                        var container = writer.database.GetContainer(typeof(T));
                        container.AddEntity(value);
                    }
                } else {
                    writer.AppendNull();
                }
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            var db = reader.entityStore;
            if (db != null && reader.parser.Level > 0) {
                if (reader.parser.Event == JsonEvent.ValueString) {
                    var id = reader.parser.value.ToString();
                    var container = db.GetContainer(typeof(T));
                    success = true;
                    return (T)container.GetEntity(id);
                }
                T entity = (T) mapper.ReadObject(ref reader, slot, out success);
                return entity;
            }
            return mapper.Read(ref reader, slot, out success);
        }
    }
    
}