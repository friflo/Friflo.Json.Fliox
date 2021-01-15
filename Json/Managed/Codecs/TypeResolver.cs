// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public class TypeResolver : ITypeResolver
    {
        public StubType CreateStubType(Type type) {
            for (int n = 0; n < resolvers.Length; n++) {
                StubType typeHandler = resolvers[n].CreateStubType(type);
                if (typeHandler != null)
                    return typeHandler;
            }
            return null;
        }
        
        private readonly IJsonCodec[] resolvers = {
            BigIntCodec.Interface,
            DateTimeCodec.Interface,
            //
            StringCodec.Interface,
            DoubleCodec.Interface,
            FloatCodec.Interface,
            LongCodec.Interface,
            IntCodec.Interface,
            ShortCodec.Interface,
            ByteCodec.Interface,
            BoolCodec.Interface,
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
        };
    }
    
    public class DebugTypeResolver : ITypeResolver {
        
        public StubType CreateStubType (Type type) {
            // find a codec manually to simplify debugging 
            StubType handler;
            
            // Specific types on top
            if ((handler = BigIntCodec.             Interface.CreateStubType(type)) != null) return handler;
            if ((handler = DateTimeCodec.           Interface.CreateStubType(type)) != null) return handler;
            
            //
            if ((handler = StringCodec.             Interface.CreateStubType(type)) != null) return handler;
            if ((handler = DoubleCodec.             Interface.CreateStubType(type)) != null) return handler;
            if ((handler = FloatCodec.              Interface.CreateStubType(type)) != null) return handler;
            if ((handler = LongCodec.               Interface.CreateStubType(type)) != null) return handler;
            if ((handler = IntCodec.                Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ShortCodec.              Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ByteCodec.               Interface.CreateStubType(type)) != null) return handler;
            if ((handler = BoolCodec.               Interface.CreateStubType(type)) != null) return handler;
            //
            if ((handler = StringArrayCodec.        Interface.CreateStubType(type)) != null) return handler;
            if ((handler = LongArrayCodec.          Interface.CreateStubType(type)) != null) return handler;
            if ((handler = IntArrayCodec.           Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ShortArrayCodec.         Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ByteArrayCodec.          Interface.CreateStubType(type)) != null) return handler;
            if ((handler = BoolArrayCodec.          Interface.CreateStubType(type)) != null) return handler;
            if ((handler = DoubleArrayCodec.        Interface.CreateStubType(type)) != null) return handler;
            if ((handler = FloatArrayCodec.         Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ObjectArrayCodec.        Interface.CreateStubType(type)) != null) return handler;
            //
            if ((handler = ListCodec.               Interface.CreateStubType(type)) != null) return handler;
            if ((handler = MapCodec.                Interface.CreateStubType(type)) != null) return handler;
            if ((handler = ObjectCodec.             Interface.CreateStubType(type)) != null) return handler;

            return null;
        }

    }
}

