// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

// using ReadResolver = System.Func<Friflo.Json.Managed.JsonReader, object, Friflo.Json.Managed.Prop.NativeType, object>;

namespace Friflo.Json.Managed.Types
{

    public class CollectionType : StubType
    {
        public   readonly   Type            keyType;
        public   readonly   int             rank;
        public              StubType        ElementType { get; private set; }
        private  readonly   Type            elementTypeNative;
        public   readonly   SimpleType.Id ? id;
        internal readonly   ConstructorInfo constructor;

    
        internal CollectionType (
                Type            type,
                Type            elementType,
                // Type         elementType,
                IJsonCodec      codec,
                int             rank,
                Type            keyType,
                ConstructorInfo constructor) :
            base (type, codec) {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            this.id             = SimpleType.IdFromType(elementType);
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