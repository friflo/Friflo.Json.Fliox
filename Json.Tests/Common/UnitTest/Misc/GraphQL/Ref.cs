using System;
using System.Reflection;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public class Entity {
        public string   id;
    }

    public class Ref<T> where T : Entity
    {
        // either id or entity is set. Never both
        public string   id;
        public T        entity;
        
        public static implicit operator Ref<T>(T entity) {
            var reference = new Ref<T>();
            reference.entity    = entity;
            return reference;
        }
        
        public static implicit operator Ref<T>(string id) {
            var reference = new Ref<T>();
            reference.id    = id;
            return reference;
        }
    }
    
    // -------------------------------------------------------------------------------------
    public class RefMatcher : ITypeMatcher {
        public static readonly RefMatcher Instance = new RefMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof(Ref<>) );
            if (args == null)
                return null;
            
            Type refType = args[0];
            ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
            
            object[] constructorParams = {config, type, constructor};
            // new RefMapper<T>(config, type, constructor);
            return (TypeMapper) TypeMapperUtils.CreateGenericInstance(typeof(RefMapper<>), new[] {refType}, constructorParams);
            
        }
    }
    
    public class RefMapper<T> : TypeMapper<Ref<T>> where T : Entity
    {
        public override string DataTypeName() { return "Ref<>"; }

        public RefMapper(StoreConfig config, Type type, ConstructorInfo constructor) :
            base(config, type, true, true)
        {
            
        }

        public override void Write(ref Writer writer, Ref<T> value) {
            writer.WriteString(value.ToString());
        }

        public override Ref<T> Read(ref Reader reader, Ref<T> slot, out bool success) {
            ref var value = ref reader.parser.value;
            success = false;
            return null;
        }
    }
    
}