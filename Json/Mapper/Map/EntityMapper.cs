using System;
using System.Reflection;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    // -------------------------------------------------------------------------------------
    
    public static class EntityMatcher {
        
        public static TypeMapper GetRefMapper(Type type, StoreConfig config, TypeMapper mapper) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            if (!typeof(Entity).IsAssignableFrom(type))
                return null;
            
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor, mapper};
            // new EntityMapper<T>(config, type, constructor, mapper);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(EntityMapper<>), new[] {type}, constructorParams);
        }
    }


    public class EntityMapper<T> : TypeMapper<T> where T : Entity
    {
        private readonly TypeMapper entityMapper;
        
        public override string DataTypeName() { return "Entity"; }

        public EntityMapper(StoreConfig config, Type type, ConstructorInfo constructor, TypeMapper mapper) :
            base(config, type, true, true)
        {
            entityMapper = mapper;
        }
        
        public override void Write(ref Writer writer, T value) {
            if (value != null) {
                writer.WriteString(value.id);
                // entityMapper.WriteObject(ref writer, value);
            } else {
                writer.AppendNull();
            }
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            T entity = (T)entityMapper.ReadObject(ref reader, slot, out success);
            return entity;
        }
    }
    
}