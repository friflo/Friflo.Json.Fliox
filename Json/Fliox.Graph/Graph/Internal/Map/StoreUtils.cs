// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Graph.Internal.Map
{
    internal static class StoreUtils
    {
        internal static Type[] GetEntityTypes<TEntityStore>() where TEntityStore : EntityStore 
        {
            var types   = new List<Type>();
            var type    = typeof(TEntityStore);
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field      = fields[n];
                Type fieldType  = field.FieldType;
                bool isEntitySet = IsEntitySet(fieldType);
                if (!isEntitySet)
                    continue;
                var genericArgs = fieldType.GetGenericArguments();
                var entityType = genericArgs[1];
                types.Add(entityType);
            }
            return types.ToArray();
        }

        internal static void InitEntitySets(EntityStore store) {
            var type    = store.GetType();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field          = fields[n];
                Type fieldType      = field.FieldType;
                bool isEntitySet    = IsEntitySet(fieldType);
                if (!isEntitySet)
                    continue;
                var setType     = field.FieldType;
                var setMapper   = (IEntitySetMapper)store._intern.typeStore.GetTypeMapper(setType);
                var entitySet   = setMapper.CreateEntitySet();
                field.SetValue(store, entitySet);
                entitySet.Init(store);
            }
        }
        
        internal static bool IsEntitySet (Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<,>);
        }
    }
}