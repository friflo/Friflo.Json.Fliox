// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Graph.Internal.Map
{
    internal static class StoreUtils
    {
        public static FieldInfo[] GetEntitySetFields(Type entityStoreType) 
        {
            var setFields   = new List<FieldInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = entityStoreType.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field      = fields[n];
                Type fieldType  = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(EntitySet<,>)) {
                    setFields.Add(field);
                }
            }
            return setFields.ToArray();
        }
        
        public static Type[] GetEntityTypes(Type entityStoreType) 
        {
            var entitySetTypes = GetEntitySetFields (entityStoreType);
            var types   = new List<Type>();
            foreach (var entitySetType in entitySetTypes) {
                var genericArgs = entitySetType.FieldType.GetGenericArguments();
                var entityType = genericArgs[1];
                types.Add(entityType);
                
            }
            return types.ToArray();
        }

        public static void InitEntitySets(EntityStore store) {
            var fields = GetEntitySetFields(store.GetType());
            foreach (var field in fields) {
                // var setType = field.FieldType;
                // var setMapper = (IEntitySetMapper)store._intern.typeStore.GetTypeMapper(setType);
                // var entitySet = setMapper.CreateEntitySet(entityStore);
                var entitySet = (EntitySet)field.GetValue(store);
                entitySet.Init(store);
            }
        }
    }
}