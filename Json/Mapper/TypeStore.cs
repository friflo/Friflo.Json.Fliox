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
        private     readonly    Dictionary <Type,  ITypeMapper> typeMap=        new Dictionary <Type,  ITypeMapper >();
        //
        internal    readonly    Dictionary <Bytes, ITypeMapper> nameToType=     new Dictionary <Bytes, ITypeMapper >();
        internal    readonly    Dictionary <Type,  Bytes>       typeToName =    new Dictionary <Type,  Bytes >();
        
        private     readonly    List<ITypeMapper>               newTypes =      new List<ITypeMapper>();


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

        internal ITypeMapper GetType (Type type)
        {
            lock (this)
            {
                ITypeMapper stubType = GetStubType(type);

                while (newTypes.Count > 0) {
                    int lastPos = newTypes.Count - 1;
                    ITypeMapper last = newTypes[lastPos];
                    newTypes.RemoveAt(lastPos);
                    // Deferred initialization of StubType references by their related Type to allow circular type dependencies.
                    // So it supports type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
                    last.InitStubType(this);
                }
                if (stubType != null)
                    return stubType;
                
                throw new NotSupportedException($"Type not supported: " + type);
            }
        }
        
        private ITypeMapper GetStubType(Type type) {
            storeLookupCount++;
            if (typeMap.TryGetValue(type, out ITypeMapper stubType))
                return stubType;
            
            typeCreationCount++;
            stubType = typeResolver.CreateStubType(type);
            if (stubType == null)
                stubType = TypeNotSupportedMatcher.CreateTypeNotSupported(type);

            
            typeMap.Add(type, stubType);
            newTypes.Add(stubType);
            return stubType;
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
                    if (nameToType.TryGetValue(bytesName, out ITypeMapper stubType)) {
                        if (type != stubType.GetNativeType())
                            throw new InvalidOperationException("Another type is already registered with this name: " + name);
                        return;
                    }
                    stubType = GetType(type);
                    Bytes discriminator = new Bytes(name);
                    typeToName.Add(stubType.GetNativeType(), discriminator);
                    nameToType.Add(discriminator, stubType);
                }
            }
        }

    }
}