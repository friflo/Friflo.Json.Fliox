// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

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
            BigIntCodec.Interface,
            //
            StringArrayCodec.Interface,
            LongArrayCodec.Interface,
            IntArrayCodec.Interface,
            ShortArrayCodec.Interface,
            ByteArrayCodec.Interface,
            BoolArrayCodec.Interface,
            DoubleArrayCodec.Interface,
            FloatArrayCodec.Interface,
            ObjectArrayCodec.Interface,
            //
            ListCodec.Interface,
            MapCodec.Interface,
            ObjectCodec.Interface,
            //
            TypeNotSupportedCodec.Interface // Should be last
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
            if ((handler = BigIntCodec.             Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = DateTimeCodec.           Interface.CreateHandler(this, type)) != null) return handler;
            
            //
            if ((handler = StringCodec.             Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleCodec.             Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatCodec.              Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongCodec.               Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntCodec.                Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortCodec.              Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteCodec.               Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolCodec.               Interface.CreateHandler(this, type)) != null) return handler;
            //
            if ((handler = StringArrayCodec.        Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = LongArrayCodec.          Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = IntArrayCodec.           Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ShortArrayCodec.         Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ByteArrayCodec.          Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = BoolArrayCodec.          Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = DoubleArrayCodec.        Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = FloatArrayCodec.         Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectArrayCodec.        Interface.CreateHandler(this, type)) != null) return handler;
            //
            if ((handler = ListCodec.               Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = MapCodec.                Interface.CreateHandler(this, type)) != null) return handler;
            if ((handler = ObjectCodec.             Interface.CreateHandler(this, type)) != null) return handler;
            //
            handler = TypeNotSupportedCodec.        Interface.CreateHandler(this, type); // Should be last

            return handler;
        }

    }
    
    // -------------------------------------------------------------------------------


}