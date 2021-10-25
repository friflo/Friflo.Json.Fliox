// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;

namespace Friflo.Json.Fliox.DB.Client.Internal.Map
{
    internal sealed class FlioxClientMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (!type.IsSubclassOf(typeof(FlioxClient)))
                return null;

            object[] constructorParams = {config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(FlioxClientMapper<>), new[] {type}, constructorParams);
        }
    }
    
    internal sealed class FlioxClientMapper<T> : TypeMapper<T>
    {
        public  override    bool    IsComplex => true;

        public FlioxClientMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            using (var fields = new PropertyFields(type, typeStore, true)) {
                FieldInfo fieldInfo = typeof(TypeMapper).GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
                // ReSharper disable once PossibleNullReferenceException
                fieldInfo.SetValue(this, fields);
            }
            var commands = CommandUtils.GetCommandTypes(type);
            foreach (var command in commands) {
                if (command.valueType != null)
                    typeStore.GetTypeMapper(command.valueType);
                if (command.resultType != null)
                    typeStore.GetTypeMapper(command.resultType);
            }
        }
        
        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }
}