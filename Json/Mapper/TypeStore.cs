// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper
{
    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="JsonReader"/> and <see cref="JsonWriter"/> instances.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeStore : IDisposable
    {
        private     readonly    Dictionary <Type,  TypeMapper> typeMap=        new Dictionary <Type,  TypeMapper >();
        //
        internal    readonly    Dictionary <Bytes, TypeMapper> nameToType=     new Dictionary <Bytes, TypeMapper >();
        internal    readonly    Dictionary <Type,  Bytes>      typeToName =    new Dictionary <Type,  Bytes >();
        
        private     readonly    List<TypeMapper>               newTypes =      new List<TypeMapper>();


        private     readonly    ITypeResolver                   typeResolver;

        public                  int                             typeCreationCount;
        public                  int                             storeLookupCount;

        public TypeStore() {
            typeResolver = new DefaultTypeResolver();
        }
        
        public TypeStore(ITypeResolver resolver) {
            typeResolver = resolver;
        }
            
        public void Dispose() {
            lock (nameToType) {
                foreach (var item in typeMap.Values)
                    item.Dispose();
                foreach (var item in typeToName.Values)
                    item.Dispose();
            }
        }

        internal TypeMapper GetTypeMapper (Type type)
        {
            lock (this)
            {
                TypeMapper mapper = GetOrCreateTypeMapper(type);

                while (newTypes.Count > 0) {
                    int lastPos = newTypes.Count - 1;
                    TypeMapper last = newTypes[lastPos];
                    newTypes.RemoveAt(lastPos);
                    // Deferred initialization of StubType references by their related Type to allow circular type dependencies.
                    // So it supports type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
                    last.InitTypeMapper(this);
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
            mapper = typeResolver.CreateTypeMapper(type);
            if (mapper == null)
                mapper = TypeNotSupportedMatcher.CreateTypeNotSupported(type, "Found no TypeMapper in TypeStore");

            
            typeMap.Add(type, mapper);
            newTypes.Add(mapper);
            return mapper;
        }
            
        /// <summary>
        /// Register a polymorphic type by its discriminant. Currently this need the first member in an JSON object
        /// and its name have to be "$type". E.g.<br/>
        /// <code>
        /// { "$type": "discriminatorName", ... }
        /// </code> 
        /// </summary>
        public void RegisterType (String name, Type type)
        {
            using (var bytesName = new Bytes(name)) {
                lock (this) {
                    if (nameToType.TryGetValue(bytesName, out TypeMapper mapper)) {
                        if (type != mapper.type)
                            throw new InvalidOperationException("Another type is already registered with this name: " + name);
                        return;
                    }
                    mapper = GetTypeMapper(type);
                    Bytes discriminator = new Bytes(name);
                    typeToName.Add(mapper.type, discriminator);
                    nameToType.Add(discriminator, mapper);
                }
            }
        }

    }
}