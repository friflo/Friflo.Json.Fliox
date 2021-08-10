// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    public class EntitySetMatcher : ITypeMatcher {
        public static readonly EntitySetMatcher Instance = new EntitySetMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<>);
            if (!isEntitySet)
                return null;

            object[] constructorParams = {config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<>), new[] {type}, constructorParams);
        }
    }
    
    public class EntitySetMapper<T> : TypeMapper<T>
    {
        private             TypeMapper  elementType;
        
        public  override    bool        IsDictionary        => true;
        public  override    TypeMapper  GetElementMapper()  => elementType;
        
        public EntitySetMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var genericArgs = type.GetGenericArguments();
            var entityType  = genericArgs[0];
            elementType     = typeStore.GetTypeMapper(entityType);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }
}