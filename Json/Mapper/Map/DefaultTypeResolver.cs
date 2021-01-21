// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Arr;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map
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
            ArrayMapper.Interface,
            //  
            ListMapper.Interface,
            DictionaryMapper.Interface,
            ClassMapper.Interface,
        };
    }
    
    public class DebugTypeResolver : ITypeResolver {
        
        public StubType CreateStubType (Type type) {
            // find a codec manually to simplify debugging 
            StubType result;
            
            // Specific types on top
            if (Match(BigIntMapper.     Interface,          type, out result)) return result;
            if (Match(DateTimeMapper.   Interface,          type, out result)) return result;
            
            //
            if (Match(StringMapper.     Interface,          type, out result)) return result;
            if (Match(DoubleMapper.     Interface,          type, out result)) return result;
            if (Match(FloatMapper.      Interface,          type, out result)) return result;
            if (Match(LongMapper.       Interface,          type, out result)) return result;
            if (Match(IntMapper.        Interface,          type, out result)) return result;
            if (Match(ShortMapper.      Interface,          type, out result)) return result;
            if (Match(ByteMapper.       Interface,          type, out result)) return result;
            if (Match(BoolMapper.       Interface,          type, out result)) return result;
            // --- List
            if (Match(PrimitiveList.    DoubleInterface,    type, out result)) return result;
            if (Match(PrimitiveList.    FloatInterface,     type, out result)) return result;
            if (Match(PrimitiveList.    LongInterface,      type, out result)) return result;
            if (Match(PrimitiveList.    IntInterface,       type, out result)) return result;
            if (Match(PrimitiveList.    ShortInterface,     type, out result)) return result;
            if (Match(PrimitiveList.    ByteInterface,      type, out result)) return result;
            if (Match(PrimitiveList.    BoolInterface,      type, out result)) return result;
            
            if (Match(PrimitiveList.    DoubleNulInterface, type, out result)) return result;
            if (Match(PrimitiveList.    FloatNulInterface,  type, out result)) return result;
            if (Match(PrimitiveList.    LongNulInterface,   type, out result)) return result;
            if (Match(PrimitiveList.    IntNulInterface,    type, out result)) return result;
            if (Match(PrimitiveList.    ShortNulInterface,  type, out result)) return result;
            if (Match(PrimitiveList.    ByteNulInterface,   type, out result)) return result;
            if (Match(PrimitiveList.    BoolNulInterface,   type, out result)) return result;
            // --- array            
            if (Match(PrimitiveArray.   DoubleInterface,    type, out result)) return result;
            if (Match(PrimitiveArray.   FloatInterface,     type, out result)) return result;
            if (Match(PrimitiveArray.   LongInterface,      type, out result)) return result;
            if (Match(PrimitiveArray.   IntInterface,       type, out result)) return result;
            if (Match(PrimitiveArray.   ShortInterface,     type, out result)) return result;
            if (Match(PrimitiveArray.   ByteInterface,      type, out result)) return result;
            if (Match(PrimitiveArray.   BoolInterface,      type, out result)) return result;

            if (Match(PrimitiveArray.   DoubleNulInterface, type, out result)) return result;
            if (Match(PrimitiveArray.   FloatNulInterface,  type, out result)) return result;
            if (Match(PrimitiveArray.   LongNulInterface,   type, out result)) return result;
            if (Match(PrimitiveArray.   IntNulInterface,    type, out result)) return result;
            if (Match(PrimitiveArray.   ShortNulInterface,  type, out result)) return result;
            if (Match(PrimitiveArray.   ByteNulInterface,   type, out result)) return result;
            if (Match(PrimitiveArray.   BoolNulInterface,   type, out result)) return result;
            
            if (Match(PrimitiveArray.   StringInterface ,   type, out result)) return result;

            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if (Match(ArrayMapper.      Interface,          type, out result)) return result;
            //
            //
            if (Match(EnumMapper.       Interface,          type, out result)) return result;
            if (Match(ListMapper.       Interface,          type, out result)) return result;
            if (Match(DictionaryMapper. Interface,          type, out result)) return result;
            if (Match(ClassMapper.      Interface,          type, out result)) return result;

            return null;
        }

        private bool Match(IJsonMapper mapper, Type type, out StubType result) {
            result = mapper.CreateStubType(type);
            return result != null;
        }

    }
}

