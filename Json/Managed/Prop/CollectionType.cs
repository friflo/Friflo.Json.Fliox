// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

// using ReadResolver = System.Func<Friflo.Json.Managed.JsonReader, object, Friflo.Json.Managed.Prop.NativeType, object>;

namespace Friflo.Json.Managed.Prop
{

    public class CollectionType : NativeType
    {
        public   readonly   Type            keyType;
        public   readonly   int             rank;
        public   readonly   Type            elementType;     // use GetElementType() if NativeType is required - its cached
        private             NativeType      elementPropType; // is set on first lookup
        public   readonly   SimpleType.Id ? id;
        internal readonly   ConstructorInfo constructor;

    
        internal CollectionType (
                Type            nativeType,
                Type            elementType,
                IJsonCodec      codec,
                int             rank,
                Type            keyType,
                ConstructorInfo constructor) :
            base (nativeType, codec) {
            this.keyType        = keyType;
            this.elementType    = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            this.id             = SimpleType.IdFromType(elementType);
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
        }
        
        public NativeType GetElementType(TypeCache typeCache) {
            if (elementType == null)
                return null;
            // simply reduce lookups
            if (elementPropType == null)
                elementPropType = typeCache.GetType(elementType);
            return elementPropType;
        }

        public override Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }
    }
}