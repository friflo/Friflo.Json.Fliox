// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Hub.Client.Internal.Map
{
    internal sealed class EntitySetMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = ClientEntityUtils.IsEntitySet(type);
            if (!isEntitySet)
                return null;
            var mapper = (TypeMapper)CreateMapper(type, config);
            return mapper;
        }
        
        internal static object CreateMapper(Type type, StoreConfig config) {
            var genericArgs = type.GetGenericArguments();
            var keyType     = genericArgs[0];
            var entityType  = genericArgs[1];

            object[] constructorParams = { config, type };
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<,,>), new[] {type, keyType, entityType}, constructorParams);
        }
    }
    
    internal interface IEntitySetMapper {
        IContainerMember CreateContainerMember (Type type, string container);
    }
    
    internal sealed class EntitySetMapper<T, TKey, TEntity> : TypeMapper<T>, IEntitySetMapper where TEntity : class
    {
        private             TypeMapper      elementType;
        
        public  override    bool            IsDictionary        => true;
        public  override    TypeMapper      GetElementMapper()  => elementType;
        public  override    bool            IsNull(ref T value) => value == null;
        
        public EntitySetMapper (StoreConfig config, Type type) :
            base (config, type, false, true)
        {
            // instanceFactory = new InstanceFactory(); // abstract type - todo remove
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            Set<TEntity>.ValidateKeyType(typeof(TKey));
            var entityType  = typeof(TEntity);
            elementType     = typeStore.GetTypeMapper(entityType);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
        
        public IContainerMember  CreateContainerMember (Type type, string container) {
            return new GenericContainerMember<TKey, TEntity> (type, container);
        }
    }
    
    internal sealed class GenericContainerMember<TKey, T> :  IContainerMember where T : class
    {
        private readonly Action<FlioxClient,EntitySet<TKey,T>> setter;
            
        internal GenericContainerMember(Type type, string container) {
            FieldInfo field = type.GetField(container);
            if (field != null) {
                setter = DelegateUtils.CreateFieldSetterIL<FlioxClient,EntitySet<TKey, T>>(field);
                return;
            }
            PropertyInfo property = type.GetProperty(container);
            setter = DelegateUtils.CreateMemberSetter<FlioxClient,EntitySet<TKey, T>>(property);
        }
        
        public void SetContainerMember(FlioxClient client, int index) {
            setter(client, new EntitySet<TKey, T>(client, index));
        }
        
        public Set CreateInstance(string container, int index, FlioxClient client) {
            var result = new Set<TKey,T>(container, index, client) {
                WritePretty = client.Options.writePretty,
                WriteNull   = client.Options.writeNull,
            };
            return result;
        }
    }

    internal interface IContainerMember
    {
        void    SetContainerMember(FlioxClient client, int index);
        Set     CreateInstance(string container, int index, FlioxClient client);
    }
}