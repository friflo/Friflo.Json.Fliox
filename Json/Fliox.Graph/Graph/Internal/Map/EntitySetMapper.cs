// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;

namespace Friflo.Json.Fliox.Graph.Internal.Map
{
    internal class EntitySetMatcher : ITypeMatcher {
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            bool isEntitySet = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<,>);
            if (!isEntitySet)
                return null;
            var genericArgs = type.GetGenericArguments();
            var keyType     = genericArgs[0];
            var entityType  = genericArgs[1];
            
            object[] constructorParams = {config, type, keyType};
            return (TypeMapper)TypeMapperUtils.CreateGenericInstance(typeof(EntitySetMapper<,>), new[] {type, entityType}, constructorParams);
        }
        
        internal static readonly object[] NoArgs = {};
    }
    
    internal interface IEntitySetMapper {
        EntitySet   CreateEntitySet ();
    }
    
    internal class EntitySetMapper<T, TEntity> : TypeMapper<T>, IEntitySetMapper    where T       : class
                                                                                    where TEntity : class
    {
        private             TypeMapper      elementType;
        private readonly    ConstructorInfo setConstructor;
        private readonly    Type            keyType;
        
        public  override    bool            IsDictionary        => true;
        public  override    TypeMapper      GetElementMapper()  => elementType;
        
        public EntitySetMapper (StoreConfig config, Type type, Type keyType) :
            base (config, type, true, false)
        {
            instanceFactory = new InstanceFactory(); // abstract type - todo remove
            setConstructor  = type.GetConstructor(new Type[] {});
            this.keyType    = keyType;
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            var entityType  = typeof(TEntity);
            elementType     = typeStore.GetTypeMapper(entityType);
        }

        public override void Write(ref Writer writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            throw new NotImplementedException();
        }
        
        public EntitySet CreateEntitySet() {
            EntitySetBase<TEntity>.ValidateKeyType(keyType);
            var instance    = setConstructor.Invoke (EntitySetMatcher.NoArgs);
            return (EntitySet)instance;
        }
    }
}