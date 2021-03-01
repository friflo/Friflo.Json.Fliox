using System;
using System.Reflection;
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

        public override void InitTypeMapper(TypeStore typeStore) {
            mapper.InitTypeMapper(typeStore);
        }
        
        public override void Write(ref Writer writer, T value) {
            if (value != null) {
                // writer.WriteString(value.id);
                mapper.WriteObject(ref writer, value);
            } else {
                writer.AppendNull();
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            T entity = (T)mapper.ReadObject(ref reader, slot, out success);
            if (success)
                reader.AddEntity(entity);
            return entity;
        }
    }
    
}