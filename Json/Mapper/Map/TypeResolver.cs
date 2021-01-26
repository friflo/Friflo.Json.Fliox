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
        TypeMapper      CreateTypeMapper(Type type);
        ResolverConfig  GetConfig();
    }
    
    /// <summary>
    /// An immutable configuration class for settings which are used by the lifetime of a <see cref="TypeStore"/>  
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ResolverConfig {
        // ReSharper disable once InconsistentNaming
        public readonly bool useIL = false;

        // ReSharper disable once InconsistentNaming
        public ResolverConfig(bool useIL) {
            this.useIL = useIL;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DefaultTypeResolver : ITypeResolver
    {
        private readonly ResolverConfig      config;
        
        /// <summary>This matcher list is not used by the type resolver itself. Its only available for debugging purposes.</summary>
        public  readonly List<ITypeMatcher>  matcherList =            new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  specificTypeMatcher =   new List<ITypeMatcher>();
        private readonly List<ITypeMatcher>  genericTypeMatcher =    new List<ITypeMatcher>();

        public ResolverConfig GetConfig() { return config; }
        
        public DefaultTypeResolver() : this (new ResolverConfig(useIL: false)) {
        }
        
        public DefaultTypeResolver(ResolverConfig config) {
            this.config = config;
            UpdateMapperList();
        }

        public TypeMapper CreateTypeMapper(Type type) {
            var query = new Query(Mode.Search);
            return QueryTypeMapper(type, query);
        }
        
        // find a codec manually to simplify debugging
        private TypeMapper QueryTypeMapper (Type type, Query q) {

            if (MatchMappers(specificTypeMatcher,       type, q)) return q.hit;
            
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
            
            if (MatchMappers(genericTypeMatcher,        type, q)) return q.hit;
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
            specificTypeMatcher.Add(mapper);
            UpdateMapperList();
        }
        
        public void AddGenericTypeMapper(ITypeMatcher mapper) {
            genericTypeMatcher.Add(mapper);
            UpdateMapperList();
        }

        private bool MatchMappers(List<ITypeMatcher> mappers, Type type, Query query) {
            for (int i = 0; i < mappers.Count; i++) {
                if (Match(mappers[i], type, query))
                    return true;
            }
            return false;
        }

        private bool Match(ITypeMatcher matcher, Type type, Query query) {
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
            QueryTypeMapper(null, query);
        }


    }
}

