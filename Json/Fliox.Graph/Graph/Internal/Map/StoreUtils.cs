// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Graph.Internal.Map
{
    internal static class StoreUtils
    {
        public static Type[] GetEntitySetTypes(Type entityStoreType) 
        {
            var types   = new List<Type>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = entityStoreType.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field      = fields[n];
                Type fieldType  = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(EntitySet<,>)) {
                    types.Add(fieldType);
                }
            }
            return types.ToArray();
        }
        
        public static Type[] GetEntityTypes(Type entityStoreType) 
        {
            var entitySetTypes = GetEntitySetTypes (entityStoreType);
            var types   = new List<Type>();
            foreach (var entitySetType in entitySetTypes) {
                var genericArgs = entitySetType.GetGenericArguments();
                var entityType = genericArgs[1];
                types.Add(entityType);
                
            }
            return types.ToArray();
        }
    }
}