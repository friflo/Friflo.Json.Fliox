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
            // --- List
            if ((stubType = PrimitiveList.            DoubleInterface.CreateStubType(type))    != null) return stubType;
            if ((stubType = PrimitiveList.            FloatInterface.CreateStubType(type))     != null) return stubType;
            if ((stubType = PrimitiveList.            LongInterface.CreateStubType(type))      != null) return stubType;
            if ((stubType = PrimitiveList.            IntInterface.CreateStubType(type))       != null) return stubType;
            if ((stubType = PrimitiveList.            ShortInterface.CreateStubType(type))     != null) return stubType;
            if ((stubType = PrimitiveList.            ByteInterface.CreateStubType(type))      != null) return stubType;
            if ((stubType = PrimitiveList.            BoolInterface.CreateStubType(type))      != null) return stubType;
            
            if ((stubType = PrimitiveList.            DoubleNulInterface.CreateStubType(type)) != null) return stubType;
            if ((stubType = PrimitiveList.            FloatNulInterface.CreateStubType(type))  != null) return stubType;
            if ((stubType = PrimitiveList.            LongNulInterface.CreateStubType(type))   != null) return stubType;
            if ((stubType = PrimitiveList.            IntNulInterface.CreateStubType(type))    != null) return stubType;
            if ((stubType = PrimitiveList.            ShortNulInterface.CreateStubType(type))  != null) return stubType;
            if ((stubType = PrimitiveList.            ByteNulInterface.CreateStubType(type))   != null) return stubType;
            if ((stubType = PrimitiveList.            BoolNulInterface.CreateStubType(type))   != null) return stubType;
            // --- array            
            if ((stubType = PrimitiveArray.           DoubleInterface.CreateStubType(type))    != null) return stubType;
            if ((stubType = PrimitiveArray.           FloatInterface.CreateStubType(type))     != null) return stubType;
            if ((stubType = PrimitiveArray.           LongInterface.CreateStubType(type))      != null) return stubType;
            if ((stubType = PrimitiveArray.           IntInterface.CreateStubType(type))       != null) return stubType;
            if ((stubType = PrimitiveArray.           ShortInterface.CreateStubType(type))     != null) return stubType;
            if ((stubType = PrimitiveArray.           ByteInterface.CreateStubType(type))      != null) return stubType;
            if ((stubType = PrimitiveArray.           BoolInterface.CreateStubType(type))      != null) return stubType;

            if ((stubType = PrimitiveArray.           DoubleNulInterface.CreateStubType(type)) != null) return stubType;
            if ((stubType = PrimitiveArray.           FloatNulInterface.CreateStubType(type))  != null) return stubType;
            if ((stubType = PrimitiveArray.           LongNulInterface.CreateStubType(type))   != null) return stubType;
            if ((stubType = PrimitiveArray.           IntNulInterface.CreateStubType(type))    != null) return stubType;
            if ((stubType = PrimitiveArray.           ShortNulInterface.CreateStubType(type))  != null) return stubType;
            if ((stubType = PrimitiveArray.           ByteNulInterface.CreateStubType(type))   != null) return stubType;
            if ((stubType = PrimitiveArray.           BoolNulInterface.CreateStubType(type))   != null) return stubType;
            
            if ((stubType = PrimitiveArray.           StringInterface.CreateStubType(type))    != null) return stubType;

            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if ((stubType = ArrayMapper.              Interface.CreateStubType(type)) != null) return stubType;
            //
            if ((stubType = EnumMapper.               Interface.CreateStubType(type)) != null) return stubType;
            //
            if ((stubType = ListMapper.               Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = DictionaryMapper.         Interface.CreateStubType(type)) != null) return stubType;
            if ((stubType = ClassMapper.              Interface.CreateStubType(type)) != null) return stubType;

            return null;
        }

    }
}

