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
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DefaultTypeResolver : ITypeResolver
    {
        /// <summary>This mapper list is not used by the type resolver itself. Its only available for debugging purposes.</summary>
        public  readonly List<ITypeMatcher>  mapperList =            new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  specificTypeMappers =   new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  genericTypeMappers =    new List<ITypeMatcher>();
        
        public DefaultTypeResolver() {
            UpdateMapperList();
        }

        public StubType CreateStubType(Type type) {
            var query = new Query(Mode.Search);
            return QueryStubType(type, query);
        }
        
        // find a codec manually to simplify debugging
        private StubType QueryStubType (Type type, Query q) {

            if (MatchMappers(specificTypeMappers,       type, q)) return q.hit;
            
            // Specific types on top
            if (Match(BigIntMatcher.        Instance,   type, q)) return q.hit;
            if (Match(DateTimeMatcher.      Instance,   type, q)) return q.hit;
                
            //  
            if (Match(StringMatcher.        Instance,   type, q)) return q.hit;
            if (Match(DoubleMatcher.        Instance,   type, q)) return q.hit;
            if (Match(FloatMatcher.         Instance,   type, q)) return q.hit;
            if (Match(LongMatcher.          Instance,   type, q)) return q.hit;
            if (Match(IntMatcher.           Instance,   type, q)) return q.hit;
            if (Match(ShortMatcher.         Instance,   type, q)) return q.hit;
            if (Match(ByteMatcher.          Instance,   type, q)) return q.hit;
            if (Match(BoolMatcher.          Instance,   type, q)) return q.hit;
            // --- List's
            if (Match(PrimitiveListMatcher. Instance,   type, q)) return q.hit;

            // --- array's           
            if (Match(PrimitiveArrayMatcher.Instance,   type, q)) return q.hit;
            
            if (MatchMappers(genericTypeMappers,        type, q)) return q.hit;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if (Match(ArrayMatcher.         Instance,   type, q)) return q.hit;
            //
            //
            if (Match(EnumMatcher.          Instance,   type, q)) return q.hit;
            if (Match(ListMatcher.          Instance,   type, q)) return q.hit;
            if (Match(DictionaryMatcher.    Instance,   type, q)) return q.hit;
            if (Match(ClassMatcher.         Instance,   type, q)) return q.hit;

            return null;
        }
        
        public void AddSpecificTypeMapper(ITypeMatcher mapper) {
            specificTypeMappers.Add(mapper);
            UpdateMapperList();
        }
        
        public void AddGenericTypeMapper(ITypeMatcher mapper) {
            genericTypeMappers.Add(mapper);
            UpdateMapperList();
        }

        private static bool MatchMappers(List<ITypeMatcher> mappers, Type type, Query query) {
            for (int i = 0; i < mappers.Count; i++) {
                if (Match(mappers[i], type, query))
                    return true;
            }
            return false;
        }

        private static bool Match(ITypeMatcher mapper, Type type, Query query) {
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
            public          List<ITypeMatcher>  mappers;
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

