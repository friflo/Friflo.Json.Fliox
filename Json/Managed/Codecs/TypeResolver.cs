// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
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
            NativeType nativeType = typeStore.typeMap.Get(type);
            if (nativeType != null)
                return nativeType;
            
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
            ObjectCodec.Resolver
        }; 
        
        private NativeType CreateType (Type type) {
            /* for (int n = 0; n < resolvers.Length; n++) {
                NativeType typeHandler = resolvers[n].AddTypeHandler(this, type);
                if (typeHandler != null)
                    return typeHandler;
            } */
            
            // search manually to simplify debugging 
            NativeType handler;
            
            if ((handler = StringArrayCodec.    Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongArrayCodec.      Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntArrayCodec.       Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortArrayCodec.     Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteArrayCodec.      Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolArrayCodec.      Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleArrayCodec.    Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatArrayCodec.     Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectArrayCodec.    Resolver.CreateHandler(this, type)) != null) return handler;
            
            if ((handler = ListCodec.           Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = MapCodec.            Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectCodec.         Resolver.CreateHandler(this, type)) != null) return handler;

            return null;
        }

    }
}