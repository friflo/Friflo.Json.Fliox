// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    public class EntitySetMatcher : ITypeMatcher {
        public static readonly EntitySetMatcher Instance = new EntitySetMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<>);
            if (!isEntitySet)
                return null;
                
            var genericArgs = type.GetGenericArguments();
            var entityType = genericArgs[0];

            object[] constructorParams = {config, entityType };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<>), new[] {type}, constructorParams);
        }
    }
    
    public class EntitySetMapper<T> : TypeMapper<T>
    {
        public EntitySetMapper (StoreConfig config, Type type) :
            base (config, type, true, false) {
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            typeStore.GetTypeMapper(type);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }
}