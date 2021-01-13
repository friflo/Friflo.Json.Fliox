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

        public NativeType CreateNativeType (Type type) {
            return Create (type);
        }

        private NativeType Create(Type type) {
            NativeType nativeType = CreateType(type);
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
            NativeType handler;
            /*
            handler = typeStore.GetType(type);
            if (handler != null)
                return handler; */
            
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