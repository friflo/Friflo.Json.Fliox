// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Managed.Map.Arr;
using Friflo.Json.Managed.Map.Obj;
using Friflo.Json.Managed.Map.Val;
using Friflo.Json.Managed.Types;

namespace Friflo.Json.Managed.Map
{
    public class DefaultTypeResolver : TypeResolver
    {
        public DefaultTypeResolver() : base (DefaultResolvers) {
        }
        
        private static readonly IJsonMapper[] DefaultResolvers = {
            BigIntMapper.Interface,
            DateTimeMapper.Interface,
            //
            StringMapper.Interface,
            DoubleMapper.Interface,
            FloatMapper.Interface,
            LongMapper.Interface,
            IntMapper.Interface,
            ShortMapper.Interface,
            ByteMapper.Interface,
            BoolMapper.Interface,
            //  
            StringArrayMapper.Interface,
            LongArrayMapper.Interface,
            IntArrayMapper.Interface,
            ShortArrayMapper.Interface,
            ByteArrayMapper.Interface,
            BoolArrayMapper.Interface,
            DoubleArrayMapper.Interface,
            FloatArrayMapper.Interface,
            ObjectArrayMapper.Interface,
            //  
            ListMapper.Interface,
            DictionaryMapper.Interface,
            ClassMapper.Interface,
        };
    }
    
    public class DebugTypeResolver : ITypeResolver {
        
        public StubType CreateStubType (Type type) {
            // find a codec manually to simplify debugging 
            StubType stubType;
            
            // Specific types on top
            if ((stubType = BigIntMapper.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DateTimeMapper.           Interface.CreateStubType(type)) != null) return stubType;
            
            //
            if ((stubType = StringMapper.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DoubleMapper.             Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = FloatMapper.              Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = LongMapper.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = IntMapper.                Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ShortMapper.              Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ByteMapper.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = BoolMapper.               Interface.CreateStubType(type)) != null) return stubType;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if ((stubType = StringArrayMapper.        Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = LongArrayMapper.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = IntArrayMapper.           Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ShortArrayMapper.         Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ByteArrayMapper.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = BoolArrayMapper.          Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DoubleArrayMapper.        Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = FloatArrayMapper.         Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ObjectArrayMapper.        Interface.CreateStubType(type)) != null) return stubType;
            //
            if ((stubType = ListMapper.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DictionaryMapper.                Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ClassMapper.             Interface.CreateStubType(type)) != null) return stubType;

            return null;
        }

    }
}

