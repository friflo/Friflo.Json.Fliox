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
            BigIntCodec.Resolver,
            //
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
            //
            TypeNotSupportedCodec.Resolver // Should be last
        }; 
        
        private NativeType CreateType (Type type) {
            /* for (int n = 0; n < resolvers.Length; n++) {
                NativeType typeHandler = resolvers[n].AddTypeHandler(this, type);
                if (typeHandler != null)
                    return typeHandler;
            } */
            
            // search manually to simplify debugging 
            NativeType handler;
            
            // Specific types on top
            if ((handler = BigIntCodec.             Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = DateTimeCodec.           Resolver.CreateHandler(this, type)) != null) return handler;
            
            //
            if ((handler = StringCodec.             Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleCodec.             Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatCodec.              Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongCodec.               Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntCodec.                Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortCodec.              Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteCodec.               Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolCodec.               Resolver.CreateHandler(this, type)) != null) return handler;
            //
            if ((handler = StringArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntArrayCodec.           Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortArrayCodec.         Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolArrayCodec.          Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatArrayCodec.         Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectArrayCodec.        Resolver.CreateHandler(this, type)) != null) return handler;
            //
            if ((handler = ListCodec.               Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = MapCodec.                Resolver.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectCodec.             Resolver.CreateHandler(this, type)) != null) return handler;
            //
            handler = TypeNotSupportedCodec.        Resolver.CreateHandler(this, type); // Should be last

            return handler;
        }

    }
    
    // -------------------------------------------------------------------------------


}