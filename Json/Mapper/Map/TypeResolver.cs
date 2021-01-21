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
            if (Match(BigIntMapper.     Interface,          type, q)) return q.hit;
            if (Match(DateTimeMapper.   Interface,          type, q)) return q.hit;
            
            //
            if (Match(StringMapper.     Interface,          type, q)) return q.hit;
            if (Match(DoubleMapper.     Interface,          type, q)) return q.hit;
            if (Match(FloatMapper.      Interface,          type, q)) return q.hit;
            if (Match(LongMapper.       Interface,          type, q)) return q.hit;
            if (Match(IntMapper.        Interface,          type, q)) return q.hit;
            if (Match(ShortMapper.      Interface,          type, q)) return q.hit;
            if (Match(ByteMapper.       Interface,          type, q)) return q.hit;
            if (Match(BoolMapper.       Interface,          type, q)) return q.hit;
            // --- List
            if (Match(PrimitiveList.    DoubleInterface,    type, q)) return q.hit;
            if (Match(PrimitiveList.    FloatInterface,     type, q)) return q.hit;
            if (Match(PrimitiveList.    LongInterface,      type, q)) return q.hit;
            if (Match(PrimitiveList.    IntInterface,       type, q)) return q.hit;
            if (Match(PrimitiveList.    ShortInterface,     type, q)) return q.hit;
            if (Match(PrimitiveList.    ByteInterface,      type, q)) return q.hit;
            if (Match(PrimitiveList.    BoolInterface,      type, q)) return q.hit;
            
            if (Match(PrimitiveList.    DoubleNulInterface, type, q)) return q.hit;
            if (Match(PrimitiveList.    FloatNulInterface,  type, q)) return q.hit;
            if (Match(PrimitiveList.    LongNulInterface,   type, q)) return q.hit;
            if (Match(PrimitiveList.    IntNulInterface,    type, q)) return q.hit;
            if (Match(PrimitiveList.    ShortNulInterface,  type, q)) return q.hit;
            if (Match(PrimitiveList.    ByteNulInterface,   type, q)) return q.hit;
            if (Match(PrimitiveList.    BoolNulInterface,   type, q)) return q.hit;
            // --- array            
            if (Match(PrimitiveArray.   DoubleInterface,    type, q)) return q.hit;
            if (Match(PrimitiveArray.   FloatInterface,     type, q)) return q.hit;
            if (Match(PrimitiveArray.   LongInterface,      type, q)) return q.hit;
            if (Match(PrimitiveArray.   IntInterface,       type, q)) return q.hit;
            if (Match(PrimitiveArray.   ShortInterface,     type, q)) return q.hit;
            if (Match(PrimitiveArray.   ByteInterface,      type, q)) return q.hit;
            if (Match(PrimitiveArray.   BoolInterface,      type, q)) return q.hit;

            if (Match(PrimitiveArray.   DoubleNulInterface, type, q)) return q.hit;
            if (Match(PrimitiveArray.   FloatNulInterface,  type, q)) return q.hit;
            if (Match(PrimitiveArray.   LongNulInterface,   type, q)) return q.hit;
            if (Match(PrimitiveArray.   IntNulInterface,    type, q)) return q.hit;
            if (Match(PrimitiveArray.   ShortNulInterface,  type, q)) return q.hit;
            if (Match(PrimitiveArray.   ByteNulInterface,   type, q)) return q.hit;
            if (Match(PrimitiveArray.   BoolNulInterface,   type, q)) return q.hit;
            
            if (Match(PrimitiveArray.   StringInterface ,   type, q)) return q.hit;

            
            if (MatchMappers(genericTypeMappers, type, q))               return q.hit;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if (Match(ArrayMapper.      Interface,          type, q)) return q.hit;
            //
            //
            if (Match(EnumMapper.       Interface,          type, q)) return q.hit;
            if (Match(ListMapper.       Interface,          type, q)) return q.hit;
            if (Match(DictionaryMapper. Interface,          type, q)) return q.hit;
            if (Match(ClassMapper.      Interface,          type, q)) return q.hit;

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
                if (Match(mappers[i], type, query))
                    return true;
            }
            return false;
        }

        private static bool Match(IJsonMapper mapper, Type type, Query query) {
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

