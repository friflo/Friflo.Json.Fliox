// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Arr;
using Friflo.Json.Mapper.Map.Obj;
using Friflo.Json.Mapper.Map.Val;

// ReSharper disable InlineOutVariableDeclaration
namespace Friflo.Json.Mapper.Map
{
    public interface ITypeResolver {
        TypeMapper      CreateTypeMapper(StoreConfig config, Type type);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DefaultTypeResolver : ITypeResolver
    {
        
        /// <summary>This matcher list is not used by the type resolver itself. Its only available for debugging purposes.</summary>
        public  readonly List<ITypeMatcher>  matcherList =          new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  specificTypeMatcher =  new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  genericTypeMatcher =   new List<ITypeMatcher>();

      
        public DefaultTypeResolver() {
            UpdateMapperList();
        }

        public TypeMapper CreateTypeMapper(StoreConfig config, Type type) {
            var query = new Query(Mode.Search);
            return QueryTypeMapper(config, type, query);
        }
        
        // find a codec manually to simplify debugging
        private TypeMapper QueryTypeMapper (StoreConfig config, Type type, Query q) {

            if (MatchMappers(specificTypeMatcher,       config, type, q)) return q.hit;
            
            // Specific types on top
            if (Match(BigIntMatcher.        Instance,   config, type, q)) return q.hit;
            if (Match(DateTimeMatcher.      Instance,   config, type, q)) return q.hit;
                
            //  
            if (Match(StringMatcher.        Instance,   config, type, q)) return q.hit;
            if (Match(DoubleMatcher.        Instance,   config, type, q)) return q.hit;
            if (Match(FloatMatcher.         Instance,   config, type, q)) return q.hit;
            if (Match(LongMatcher.          Instance,   config, type, q)) return q.hit;
            if (Match(IntMatcher.           Instance,   config, type, q)) return q.hit;
            if (Match(ShortMatcher.         Instance,   config, type, q)) return q.hit;
            if (Match(ByteMatcher.          Instance,   config, type, q)) return q.hit;
            if (Match(BoolMatcher.          Instance,   config, type, q)) return q.hit;
            // --- List's
            if (Match(PrimitiveListMatcher. Instance,   config, type, q)) return q.hit;

            // --- array's           
            if (Match(PrimitiveArrayMatcher.Instance,   config, type, q)) return q.hit;
            
            if (MatchMappers(genericTypeMatcher,        config, type, q)) return q.hit;
            //
            // The order of codecs bellow need to be irrelevant to ensure same behavior independent
            // when adding various codecs to a custom resolver.
            if (Match(ArrayMatcher.         Instance,   config, type, q)) return q.hit;
            //
            //
            if (Match(EnumMatcher.              Instance,   config, type, q)) return q.hit;
            if (Match(ListMatcher.              Instance,   config, type, q)) return q.hit;
            if (Match(GenericIListMatcher.      Instance,   config, type, q)) return q.hit;
            if (Match(GenericICollectionMatcher.Instance,   config, type, q)) return q.hit;
            if (Match(DictionaryMatcher.        Instance,   config, type, q)) return q.hit;
            if (Match(ClassMatcher.             Instance,   config, type, q)) return q.hit;

            return null;
        }
        
        public void AddSpecificTypeMapper(ITypeMatcher mapper) {
            specificTypeMatcher.Add(mapper);
            UpdateMapperList();
        }
        
        public void AddGenericTypeMapper(ITypeMatcher mapper) {
            genericTypeMatcher.Add(mapper);
            UpdateMapperList();
        }

        private static bool MatchMappers(List<ITypeMatcher> mappers,  StoreConfig config, Type type, Query query) {
            for (int i = 0; i < mappers.Count; i++) {
                if (Match(mappers[i], config, type, query))
                    return true;
            }
            return false;
        }

        private static bool Match(ITypeMatcher matcher, StoreConfig config, Type type, Query query) {
            if (query.mode == Mode.Search) {
                query.hit = matcher.MatchTypeMapper(type, config);
                return query.hit != null;
            }

            query.matchers.Add(matcher);
            query.hit = null;
            return false;
        }
        
        enum Mode {
            Search,
            Enumerate
        }

        class Query {
            public readonly Mode                mode;
            public          List<ITypeMatcher>  matchers;
            public          TypeMapper          hit;

            public Query(Mode mode) {
                this.mode = mode;
            }
        }

        private void UpdateMapperList() {
            matcherList.Clear();
            var query = new Query(Mode.Enumerate) {
                matchers = matcherList
            };
            QueryTypeMapper(null, null, query);
        }


    }
}

