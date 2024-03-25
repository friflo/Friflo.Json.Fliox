// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

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
        public  override    bool    IsNull(ref T value)  => value == null;
        public  override    bool    IsComplex => true;
        // ReSharper disable once UnassignedReadonlyField - field ist set via reflection below to use make field readonly
        private readonly    PropertyFields<T>   propFields;
        
        public  override    PropertyFields      PropFields => propFields;
        
        public FlioxClientMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type
            propFields      = null; // suppress [CS0649] Field '...' is never assigned to, and will always have its default value null
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var query = new FieldQuery<T>(typeStore, type, null, ClientFieldFilter.Instance);
            using (var fields = new PropertyFields<T>(query)) {
                FieldInfo fieldInfo = mapperType.GetField(nameof(propFields), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // ReSharper disable once PossibleNullReferenceException
                fieldInfo.SetValue(this, fields);
            }
            
            var messageInfos = HubMessagesUtils.GetMessageInfos(type, typeStore);
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
                return hasSetter || AttributeUtils.Property(property.CustomAttributes);
            }
            if (memberInfo is FieldInfo field) {
                if (!ClientEntityUtils.IsEntitySet(field.FieldType))
                    return false;
                return field.IsPublic || AttributeUtils.Property(field.CustomAttributes);
            }
            return false;
        }
    }
}