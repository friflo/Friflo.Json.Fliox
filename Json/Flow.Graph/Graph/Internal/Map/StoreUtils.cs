// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal.Map
{
    internal static class StoreUtils
    {
        public static Type[] GetEntityTypes<TEntityStore>() where TEntityStore : EntityStore 
        {
            var types   = new List<Type>();
            var type    = typeof(TEntityStore);
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field      = fields[n];
                Type fieldType  = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(EntitySet<,>)) {
                    var genericArgs = fieldType.GetGenericArguments();
                    var entityType = genericArgs[1];
                    types.Add(entityType);
                }
            }
            return types.ToArray();
        }
    }
}