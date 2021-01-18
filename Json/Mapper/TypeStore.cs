// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper
{
    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="JsonReader"/> and <see cref="JsonWriter"/> instances.
    /// </summary>
    public class TypeStore : IDisposable
    {
        private     readonly    Dictionary <Type,  StubType>   typeMap=        new Dictionary <Type,  StubType >();
        //
        internal    readonly    Dictionary <Bytes, StubType>   nameToType=     new Dictionary <Bytes, StubType >();
        internal    readonly    Dictionary <Type,  Bytes>      typeToName =    new Dictionary <Type,  Bytes >();
        
        private     readonly    List<StubType>                  newTypes =      new List<StubType>();


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

        internal StubType GetType (Type type)
        {
            lock (this)
            {
                StubType stubType = GetStubType(type);

                while (newTypes.Count > 0) {
                    int lastPos = newTypes.Count - 1;
                    StubType last = newTypes[lastPos];
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
        
        private StubType GetStubType(Type type) {
            storeLookupCount++;
            if (typeMap.TryGetValue(type, out StubType stubType))
                return stubType;
            
            typeCreationCount++;
            stubType = typeResolver.CreateStubType(type);
            if (stubType == null)
                stubType = TypeNotSupportedMapper.Interface.CreateStubType(type);

            
            typeMap.Add(type, stubType);
            newTypes.Add(stubType);
            return stubType;
        }
            
        public void RegisterType (String name, Type type)
        {
            using (var bytesName = new Bytes(name)) {
                lock (this) {
                    if (nameToType.TryGetValue(bytesName, out StubType stubType)) {
                        if (type != stubType.type)
                            throw new InvalidOperationException("Another type is already registered with this name: " + name);
                        return;
                    }
                    stubType = GetType(type);
                    Bytes discriminator = new Bytes(name);
                    typeToName.Add(stubType.type, discriminator);
                    nameToType.Add(discriminator, stubType);
                }
            }
        }

    }
}