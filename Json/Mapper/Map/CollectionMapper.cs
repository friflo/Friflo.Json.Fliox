// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public abstract class CollectionMapper<TVal, TElm> : TypeMapper<TVal>
    {
        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection below to enable using a readonly field
        public   readonly   TypeMapper<TElm>    elementType;
        private  readonly   Type                elementTypeNative;
        private  readonly   ConstructorInfo     constructor;
        
        // ReSharper disable NotAccessedField.Local
        private  readonly   int                 rank;
        private  readonly   Type                keyType;

        internal CollectionMapper (
            Type                type,
            Type                elementType,
            int                 rank,
            Type                keyType,
            ConstructorInfo     constructor) : base (type, true)
        {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            FieldInfo fieldInfo = GetType().GetField(nameof(elementType));
            TypeMapper mapper = typeStore.GetTypeMapper(elementTypeNative);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, mapper);
        }
        
        public override Object CreateInstance ()
        {
            return ReflectUtils.CreateInstance(constructor);
        }
    }
}
