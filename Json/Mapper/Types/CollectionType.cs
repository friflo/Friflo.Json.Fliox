// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

// using ReadResolver = System.Func<Friflo.Json.Managed.JsonReader, object, Friflo.Json.Managed.Prop.NativeType, object>;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CollectionType : StubType
    {
        public   readonly   Type            keyType;
        public   readonly   int             rank;
        public              StubType        ElementType { get; private set; }
        private  readonly   Type            elementTypeNative;
        public   readonly   VarType         elementVarType;
        internal readonly   ConstructorInfo constructor;

        internal CollectionType (
            Type            type,
            Type            elementType,
            ITypeMapper     map,
            int             rank,
            Type            keyType,
            ConstructorInfo constructor) : base (type, map, true, null)
        {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            elementVarType       = Var.GetVarType(elementType);
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
        }
        
        public override void InitStubType(TypeStore typeStore) {
            ElementType = typeStore.GetType(elementTypeNative);
        }

        
        /*
        public NativeType GetElementType(TypeCache typeCache) {
            if (elementType == null)
                return null;
            // simply reduce lookups
            if (elementPropType == null)
                elementPropType = typeCache.GetType(elementType);
            return elementType;
        } */

        public override Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }
    }
}