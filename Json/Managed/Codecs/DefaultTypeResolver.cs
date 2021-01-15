// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Codecs
{
    public class DefaultTypeResolver : TypeResolver
    {
        public DefaultTypeResolver() : base (DefaultResolvers) {
        }
        
        private static readonly IJsonCodec[] DefaultResolvers = {
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
            StubType stubType;
            
            // Specific types on top
            if ((stubType = BigIntCodec.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DateTimeCodec.           Interface.CreateStubType(type)) != null) return stubType;
            
            //
            if ((stubType = StringCodec.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DoubleCodec.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = FloatCodec.              Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = LongCodec.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = IntCodec.                Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ShortCodec.              Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ByteCodec.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = BoolCodec.               Interface.CreateStubType(type)) != null) return stubType;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if ((stubType = StringArrayCodec.        Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = LongArrayCodec.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = IntArrayCodec.           Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ShortArrayCodec.         Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ByteArrayCodec.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = BoolArrayCodec.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DoubleArrayCodec.        Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = FloatArrayCodec.         Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ObjectArrayCodec.        Interface.CreateStubType(type)) != null) return stubType;
            //
            if ((stubType = ListCodec.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = MapCodec.                Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ObjectCodec.             Interface.CreateStubType(type)) != null) return stubType;

            return null;
        }

    }
}

