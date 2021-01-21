// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Arr;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Val;
using Friflo.Json.Mapper.Types;

// ReSharper disable InlineOutVariableDeclaration
namespace Friflo.Json.Mapper.Map
{
    public interface ITypeResolver {
        StubType CreateStubType(Type type);
    }
    
    public class DefaultTypeResolver : ITypeResolver
    {
        /// <summary>This mapper list is not used by the type resolver itself. Its only available for debugging purposes.</summary>
        public  readonly List<IJsonMapper>  mapperList =            new List<IJsonMapper>();
        private readonly List<IJsonMapper>  specificTypeMappers =   new List<IJsonMapper>();
        private readonly List<IJsonMapper>  genericTypeMappers =    new List<IJsonMapper>();
        
        public DefaultTypeResolver() {
            UpdateMapperList();
        }

        public StubType CreateStubType(Type type) {
            var query = new Query(Mode.Search);
            return QueryStubType(type, query);
        }
        
        // find a codec manually to simplify debugging
        private StubType QueryStubType (Type type, Query q) {

            if (MatchMappers(specificTypeMappers, type, q))               return q.hit;
            
            // Specific types on top
            if (Match(BigIntMapper.     Interface,          type, ref q)) return q.hit;
            if (Match(DateTimeMapper.   Interface,          type, ref q)) return q.hit;
            
            //
            if (Match(StringMapper.     Interface,          type, ref q)) return q.hit;
            if (Match(DoubleMapper.     Interface,          type, ref q)) return q.hit;
            if (Match(FloatMapper.      Interface,          type, ref q)) return q.hit;
            if (Match(LongMapper.       Interface,          type, ref q)) return q.hit;
            if (Match(IntMapper.        Interface,          type, ref q)) return q.hit;
            if (Match(ShortMapper.      Interface,          type, ref q)) return q.hit;
            if (Match(ByteMapper.       Interface,          type, ref q)) return q.hit;
            if (Match(BoolMapper.       Interface,          type, ref q)) return q.hit;
            // --- List
            if (Match(PrimitiveList.    DoubleInterface,    type, ref q)) return q.hit;
            if (Match(PrimitiveList.    FloatInterface,     type, ref q)) return q.hit;
            if (Match(PrimitiveList.    LongInterface,      type, ref q)) return q.hit;
            if (Match(PrimitiveList.    IntInterface,       type, ref q)) return q.hit;
            if (Match(PrimitiveList.    ShortInterface,     type, ref q)) return q.hit;
            if (Match(PrimitiveList.    ByteInterface,      type, ref q)) return q.hit;
            if (Match(PrimitiveList.    BoolInterface,      type, ref q)) return q.hit;
            
            if (Match(PrimitiveList.    DoubleNulInterface, type, ref q)) return q.hit;
            if (Match(PrimitiveList.    FloatNulInterface,  type, ref q)) return q.hit;
            if (Match(PrimitiveList.    LongNulInterface,   type, ref q)) return q.hit;
            if (Match(PrimitiveList.    IntNulInterface,    type, ref q)) return q.hit;
            if (Match(PrimitiveList.    ShortNulInterface,  type, ref q)) return q.hit;
            if (Match(PrimitiveList.    ByteNulInterface,   type, ref q)) return q.hit;
            if (Match(PrimitiveList.    BoolNulInterface,   type, ref q)) return q.hit;
            // --- array            
            if (Match(PrimitiveArray.   DoubleInterface,    type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   FloatInterface,     type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   LongInterface,      type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   IntInterface,       type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   ShortInterface,     type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   ByteInterface,      type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   BoolInterface,      type, ref q)) return q.hit;

            if (Match(PrimitiveArray.   DoubleNulInterface, type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   FloatNulInterface,  type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   LongNulInterface,   type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   IntNulInterface,    type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   ShortNulInterface,  type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   ByteNulInterface,   type, ref q)) return q.hit;
            if (Match(PrimitiveArray.   BoolNulInterface,   type, ref q)) return q.hit;
            
            if (Match(PrimitiveArray.   StringInterface ,   type, ref q)) return q.hit;

            
            if (MatchMappers(genericTypeMappers, type, q))               return q.hit;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if (Match(ArrayMapper.      Interface,          type, ref q)) return q.hit;
            //
            //
            if (Match(EnumMapper.       Interface,          type, ref q)) return q.hit;
            if (Match(ListMapper.       Interface,          type, ref q)) return q.hit;
            if (Match(DictionaryMapper. Interface,          type, ref q)) return q.hit;
            if (Match(ClassMapper.      Interface,          type, ref q)) return q.hit;

            return null;
        }
        
        public void AddSpecificTypeMapper(IJsonMapper mapper) {
            specificTypeMappers.Add(mapper);
            UpdateMapperList();
        }
        
        public void AddGenericTypeMapper(IJsonMapper mapper) {
            genericTypeMappers.Add(mapper);
            UpdateMapperList();
        }

        private static bool MatchMappers(List<IJsonMapper> mappers, Type type, Query query) {
            for (int i = 0; i < mappers.Count; i++) {
                if (Match(mappers[i], type, ref query))
                    return true;
            }
            return false;
        }

        private static bool Match(IJsonMapper mapper, Type type, ref Query query) {
            if (query.mode == Mode.Search) {
                query.hit = mapper.CreateStubType(type);
                return query.hit != null;
            }

            query.mappers.Add(mapper);
            query.hit = null;
            return false;
        }
        
        enum Mode {
            Search,
            Enumerate
        }

        class Query {
            public readonly Mode                mode;
            public          List<IJsonMapper>   mappers;
            public          StubType            hit;

            public Query(Mode mode) {
                this.mode = mode;
            }
        }

        private void UpdateMapperList() {
            mapperList.Clear();
            var query = new Query(Mode.Enumerate) {
                mappers = mapperList
            };
            QueryStubType(null, query);
        }


    }
}

