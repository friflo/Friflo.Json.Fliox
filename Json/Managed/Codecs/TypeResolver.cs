// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed.Codecs
{
    public class TypeResolver
    {
        private readonly TypeStore      typeStore;
        
        public TypeResolver(TypeStore typeStore) {
            this.typeStore = typeStore;
        }

        public NativeType GetNativeType (Type type) {
            typeStore.storeLookupCount++;
            NativeType nativeType = typeStore.typeMap.Get(type);
            if (nativeType != null)
                return nativeType;
            
            typeStore.typeCreationCount++;
            nativeType = CreateType(type);
            typeStore.typeMap.Put(type, nativeType);
            return nativeType;
        }

        private readonly IJsonCodec[] resolvers = {
            StringArrayCodec.Resolver,
            LongArrayCodec.Resolver,
            IntArrayCodec.Resolver,
            ShortArrayCodec.Resolver,
            ByteArrayCodec.Resolver,
            BoolArrayCodec.Resolver,
            DoubleArrayCodec.Resolver,
            FloatArrayCodec.Resolver,
            ObjectArrayCodec.Resolver,
            //
            ListCodec.Resolver,
            MapCodec.Resolver,
            ObjectCodec.Resolver,
            PrimitiveCodec.Resolver,
            TypeNotSupportedCodec.Resolver
        }; 
        
        private NativeType CreateType (Type type) {
            /* for (int n = 0; n < resolvers.Length; n++) {
                NativeType typeHandler = resolvers[n].AddTypeHandler(this, type);
                if (typeHandler != null)
                    return typeHandler;
            } */
            
            // search manually to simplify debugging 
            NativeType handler;
            
            if ((handler = StringArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntArrayCodec.           Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortArrayCodec.         Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatArrayCodec.         Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
                
            if ((handler = ListCodec.               Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = MapCodec.                Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectCodec.             Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = PrimitiveCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            
            handler = TypeNotSupportedCodec.        Resolver.CreateHandler(this, type);

            return handler;
        }

    }
    
    // -------------------------------------------------------------------------------
    public class TypeNotSupported : NativeType {
        public TypeNotSupported(Type type) : 
            base(type, TypeNotSupportedCodec.Resolver) {
        }

        public override object CreateInstance() {
            throw new NotSupportedException("Type not supported" + type.FullName);
        }
    }
    
    public class TypeNotSupportedCodec : IJsonCodec
    {
        public static readonly TypeNotSupportedCodec Resolver = new TypeNotSupportedCodec();

        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type.IsPrimitive)
                return new Primitive(type);
            return null;
        }

        public object Read(JsonReader reader, object obj, NativeType nativeType) {
            throw new NotSupportedException("Type not supported. type: " + nativeType.type.FullName);
        }

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            throw new NotSupportedException("Type not supported. type: " + nativeType.type.FullName);
        }
    }
    
    // -------------------------------------------------------------------------------
    public class Primitive : NativeType {
        public Primitive(Type type) : 
            base(type, PrimitiveCodec.Resolver) {
        }

        public override object CreateInstance() {
            throw new NotSupportedException("primitives don't use a codec" + type.FullName);
        }
    }
    
    public class PrimitiveCodec : IJsonCodec
    {
        public static readonly PrimitiveCodec Resolver = new PrimitiveCodec();

        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type.IsPrimitive)
                return new Primitive(type);
            return null;
        }

        public object Read(JsonReader reader, object obj, NativeType nativeType) {
            throw new InvalidOperationException("primitives don't use a codec. type: " + nativeType.type.FullName);
        }

        public void Write(JsonWriter writer, object obj, NativeType nativeType) {
            throw new InvalidOperationException("primitives don't use a codec. type: " + nativeType.type.FullName);
        }
    }
}