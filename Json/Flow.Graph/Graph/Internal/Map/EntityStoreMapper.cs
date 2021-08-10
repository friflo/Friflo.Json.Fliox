// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;

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
            var fields = new PropertyFields(type, typeStore);
            FieldInfo fieldInfo = typeof(TypeMapper).GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, fields);
        }
        
        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }
}