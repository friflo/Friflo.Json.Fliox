// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Graph.Internal.Id;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Fliox.Graph.Internal.Map
{
    internal class EntitySetMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<,>);
            if (!isEntitySet)
                return null;
            
            object[] constructorParams = {config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<>), new[] {type}, constructorParams);
        }
    }
    
    interface IEntitySetFactory {
        EntitySet   CreateEntitySet (EntityStore store);
    }
    
    internal class EntitySetMapper<T> : TypeMapper<T>, IEntitySetFactory where T : class
    {
        private             TypeMapper      elementType;
        private readonly    ConstructorInfo setConstructor;
        
        public  override    bool            IsDictionary        => true;
        public  override    TypeMapper      GetElementMapper()  => elementType;
        
        public EntitySetMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type - todo remove
            Type[] entitySetArgs = { typeof(EntityStore) };
            setConstructor = type.GetConstructor(entitySetArgs);
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var genericArgs = type.GetGenericArguments();
            var entityType  = genericArgs[1];
            elementType     = typeStore.GetTypeMapper(entityType);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
        
        public EntitySet CreateEntitySet(EntityStore store) {
            var genericArgs = type.GetGenericArguments();
            var entityType  = genericArgs[1];
            if (!EntityId.FindEntityId(entityType, out EntityId _))
                return null;
            var args = new object[] {store};
            var instance = setConstructor.Invoke (args);
            return (EntitySet)instance;
        }
    }
}