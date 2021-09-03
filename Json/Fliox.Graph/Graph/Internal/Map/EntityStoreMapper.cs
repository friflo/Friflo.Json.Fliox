// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Fliox.Db.Graph.Internal.Map
{
    internal class EntityStoreMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsSubclassOf(typeof(EntityStore)))
                return null;

            object[] constructorParams = {config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntityStoreMapper<>), new[] {type}, constructorParams);
        }
    }
    
    internal class EntityStoreMapper<T> : TypeMapper<T>
    {
        public  override    bool    IsComplex => true;

        public EntityStoreMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var fields = new PropertyFields(type, typeStore, true);
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