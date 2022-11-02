// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Hub.Client.Internal.Map
{
    internal sealed class EntitySetMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = ClientEntityUtils.IsEntitySet(type);
            if (!isEntitySet)
                return null;
            var genericArgs = type.GetGenericArguments();
            var keyType     = genericArgs[0];
            var entityType  = genericArgs[1];

            object[] constructorParams = {config, type};
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<,,>), new[] {type, keyType, entityType}, constructorParams);
        }
    }
    
    internal interface IEntitySetMapper {
        EntitySet   CreateEntitySet (string name);
    }
    
    internal sealed class EntitySetMapper<T, TKey, TEntity> : TypeMapper<T>, IEntitySetMapper where TEntity : class
    {
        private             TypeMapper      elementType;
        
        public  override    bool            IsDictionary        => true;
        public  override    TypeMapper      GetElementMapper()  => elementType;
        public  override    bool            IsNull(ref T value) => value == null;
        
        public EntitySetMapper (StoreConfig config, Type type) :
            base (config, type, true, false)
        {
            // instanceFactory = new InstanceFactory(); // abstract type - todo remove
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            EntitySetBase<TEntity>.ValidateKeyType(typeof(TKey));
            var entityType  = typeof(TEntity);
            elementType     = typeStore.GetTypeMapper(entityType);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
        
        public EntitySet CreateEntitySet(string name) {
            // EntitySetBase<TEntity>.ValidateKeyType(typeof(TKey));
            return new EntitySet<TKey,TEntity>(name);
        }
    }
}