// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;

namespace Friflo.Json.Fliox.Hub.Client.Internal.Map
{
    internal sealed class FlioxClientMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isSameOrSubclass = type == typeof(FlioxClient) || type.IsSubclassOf(typeof(FlioxClient));
            if (!isSameOrSubclass)
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
            using (var fields = new PropertyFields(type, typeStore, ClientFieldFilter.Instance)) {
                FieldInfo fieldInfo = typeof(TypeMapper).GetField(nameof(propFields), BindingFlags.Public | BindingFlags.Instance);
                // ReSharper disable once PossibleNullReferenceException
                fieldInfo.SetValue(this, fields);
            }
            
            var messageInfos = MessageUtils.GetMessageInfos(type, typeStore);
            AddMessages(typeStore, messageInfos);
        }
        
        private static void AddMessages(TypeStore typeStore, MessageInfo[] messageInfos) {
            if (messageInfos == null || messageInfos.Length == 0)
                return;
            foreach (var messageInfo in messageInfos) {
                if (messageInfo.paramType != null)
                    typeStore.GetTypeMapper(messageInfo.paramType);
                if (messageInfo.resultType != null)
                    typeStore.GetTypeMapper(messageInfo.resultType);
            }
        }
        
        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Ignore all fields / properties in a <see cref="FlioxClient"/> which are not of Type <see cref="EntitySet{TKey,T}"/>
    /// </summary>
    internal sealed class ClientFieldFilter : FieldFilter
    {
        internal static readonly ClientFieldFilter Instance = new ClientFieldFilter();

        public override bool AddField(MemberInfo memberInfo) {
            if (memberInfo is PropertyInfo property) {
                if (!ClientEntityUtils.IsEntitySet(property.PropertyType))
                    return false;
                // has public or non-public setter
                bool hasSetter = property.GetSetMethod(true) != null;
                return hasSetter || FieldQuery.Property(property.CustomAttributes);
            }
            if (memberInfo is FieldInfo field) {
                if (!ClientEntityUtils.IsEntitySet(field.FieldType))
                    return false;
                return field.IsPublic || FieldQuery.Property(field.CustomAttributes);
            }
            return false;
        }
    }
}