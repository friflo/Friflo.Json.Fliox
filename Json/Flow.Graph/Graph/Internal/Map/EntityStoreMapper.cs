// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    public class EntityStoreMatcher : ITypeMatcher {
        public static readonly EntityStoreMatcher Instance = new EntityStoreMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsSubclassOf(typeof(EntityStore)))
                return null;

            object[] constructorParams = {config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntityStoreMapper<>), new[] {type}, constructorParams);
        }
    }
    
    public class EntityStoreMapper<T> : TypeMapper<T>
    {
        public EntityStoreMapper (StoreConfig config, Type type) :
            base (config, type, true, false) {
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var entityTypes = GetEntityTypes(type);
            foreach (var entityType in entityTypes) {
                typeStore.GetTypeMapper(entityType);    
            }
        }
        
        private static Type[] GetEntityTypes(Type type) 
        {
            var types   = new List<Type>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field      = fields[n];
                Type fieldType  = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(EntitySet<>)) {
                    var genericArgs = fieldType.GetGenericArguments();
                    var entityType = genericArgs[0];
                    types.Add(entityType);
                }
            }
            return types.ToArray();
        }
        
        
        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }
}