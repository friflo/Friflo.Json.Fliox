// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

#if !UNITY_5_3_OR_NEWER
    [assembly: CLSCompliant(true)]
#endif

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary>
    /// An immutable configuration class for settings which are used by the lifetime of a <see cref="TypeStore"/>  
    /// </summary>
    [CLSCompliant(true)]
    public sealed class StoreConfig {
        internal readonly   Dictionary<Type, KeyMapper> keyMappers;
        internal            AssemblyDocs                assemblyDocs;

        public StoreConfig() {
            this.keyMappers = KeyMapper.CreateDefaultKeyMappers();
        }
        
        public KeyMapper GetKeyMapper (Type type) {
            return keyMappers[type];
        }
    }

    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="Reader"/> and <see cref="ObjectWriter"/> instances.
    /// <br/>
    /// The intention is to use only a single <see cref="TypeStore"/> instance within the whole application.
    /// </summary>
    [CLSCompliant(true)]
    public sealed class TypeStore : IDisposable
    {
        private     readonly    Dictionary <Type,  TypeMapper>  typeMap=        new Dictionary <Type,  TypeMapper >();
        
        private     readonly    List<TypeMapper>                newTypes =      new List<TypeMapper>();
        public      readonly    ITypeResolver                   typeResolver;
        public      readonly    StoreConfig                     config;
        internal    readonly    AssemblyDocs                    assemblyDocs = new AssemblyDocs();

        public                  int                             typeCreationCount;
        public                  int                             storeLookupCount;
        private                 int                             currentClassId;

        public TypeStore() {
            typeResolver    = new DefaultTypeResolver();
            config          = new StoreConfig { assemblyDocs = assemblyDocs };
        }
        
        public static readonly TypeStore Global = new TypeStore();
        
        public TypeStore(StoreConfig config = null, ITypeResolver resolver = null) {
            this.config     = config    ?? new StoreConfig { assemblyDocs = assemblyDocs };
            typeResolver    = resolver  ?? new DefaultTypeResolver();
        }
            
        public void Dispose() {
            lock (this) {
                foreach (var mapper in typeMap.Values)
                    mapper.Dispose();
            }
        }
        
        public bool AddKeyMapper (Type keyType, KeyMapper keyMapper) {
            return config.keyMappers.TryAdd(keyType, keyMapper);
        }
        
        public List<TypeMapper> AddMappers (ICollection<Type> types) {
            var list = new List<TypeMapper>();
            foreach (var type in types) {
                var mapper = GetTypeMapper(type);
                list.Add(mapper);
            }
            return list;
        }
        
        public TypeMapper<T> GetTypeMapper<T> () {
            return (TypeMapper<T>)GetTypeMapper(typeof(T));
        }

        public TypeMapper GetTypeMapper (Type type)
        {
            lock (this)
            {
                TypeMapper mapper = GetOrCreateTypeMapper(type);

                while (newTypes.Count > 0) {
                    int lastPos = newTypes.Count - 1;
                    TypeMapper last = newTypes[lastPos];
                    newTypes.RemoveAt(lastPos);
                    // Deferred initialization of TypeMapper to allow circular type dependencies.
                    // So it supports type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
                    last.InitTypeMapper(this);
                    last.classId = mapper.type.IsValueType ? - 1 : currentClassId++;
#if DEBUG
                    last.typeStore  = this;
#endif
                }
                if (mapper != null)
                    return mapper;
                
                throw new NotSupportedException($"Type not supported: " + type);
            }
        }
        
        private TypeMapper GetOrCreateTypeMapper(Type type) {
            storeLookupCount++;
            if (typeMap.TryGetValue(type, out TypeMapper mapper))
                return mapper;
            
            typeCreationCount++;

            var typeMapperType = GetTypeMapperType(type, out bool isMapper);
            if (typeMapperType != null) {
                if (isMapper) {
                    mapper = (TypeMapper)ReflectUtils.CreateInstance(typeMapperType);
                } else {
                    var matcher = (ITypeMatcher)ReflectUtils.CreateInstance(typeMapperType);
                    typeResolver.AddGenericTypeMatcher(matcher);
                    mapper = typeResolver.CreateTypeMapper(config, type);    
                }
            } else {
                mapper = typeResolver.CreateTypeMapper(config, type);
            }

            if (mapper == null)
                mapper = TypeNotSupportedMatcher.CreateTypeNotSupported(config, type, "Found no TypeMapper in TypeStore");

            
            typeMap.Add(type, mapper);
            newTypes.Add(mapper);
            return mapper;
        }
        
        private static Type GetTypeMapperType(Type type, out bool isMapper) {
            foreach (var attr in type.CustomAttributes) {
                if (attr.AttributeType == typeof(TypeMapperAttribute)) {
                    var arg = attr.ConstructorArguments;
                    var typeMapper = arg[0].Value as Type;
                    if (typeMapper != null) {
                        if (typeMapper.IsSubclassOf(typeof(TypeMapper))) {
                            isMapper = true;
                            return typeMapper;
                        }
                        if (typeof(ITypeMatcher).IsAssignableFrom(typeMapper)) {
                            isMapper = false;
                            return typeMapper;
                        }
                    }
                    var msg = $"[TypeMapper()] parameter must be a Type extending TypeMapper or TypeMatcher at Type: {type}";
                    throw new InvalidOperationException(msg);
                }
            }
            var baseType = type.BaseType;
            if (baseType != null) {
                // some types require using the ITypeMatcher from their base class.
                // E.g. classes extending FlioxClient use FlioxClientMatcher
                return GetTypeMapperType (baseType, out isMapper);
            }
            isMapper = false;
            return null;
        }

        public Dictionary<Type, TypeMapper> GetTypeMappers() {
            return new Dictionary<Type, TypeMapper>(typeMap);
        }
    }
}
