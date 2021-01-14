// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed
{
    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="JsonReader"/> and <see cref="JsonWriter"/> instances.
    /// </summary>
    public class TypeStore : IDisposable
    {
        internal  readonly  HashMapLang <Type,  StubType>   typeMap=        new HashMapLang <Type,  StubType >();
        //
        internal  readonly  HashMapLang <Bytes, StubType>   nameToType=     new HashMapLang <Bytes, StubType >();
        internal  readonly  HashMapLang <Type,  Bytes>      typeToName =    new HashMapLang <Type,  Bytes >();

        private   readonly  TypeResolver                    typeResolver;

        public              int                             typeCreationCount;
        public              int                             storeLookupCount;

        public TypeStore() {
            typeResolver = new TypeResolver(this);
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
                StubType stubType = typeResolver.GetNativeType(type);
                if (stubType != null)
                    return stubType;
                
                throw new NotSupportedException($"Type not supported: " + type.FullName);
            }
        }
            
        public void RegisterType (String name, Type type)
        {
            using (var bytesName = new Bytes(name)) {
                lock (this) {
                    StubType stubType = nameToType.Get(bytesName);
                    if (stubType != null) {
                        if (type != stubType.type)
                            throw new InvalidOperationException("Another type is already registered with this name: " + name);
                        return;
                    }
                    stubType = GetType(type);
                    Bytes discriminator = new Bytes(name);
                    typeToName.Put(stubType.type, discriminator);
                    nameToType.Put(discriminator, stubType);
                }
            }
        }

    }
}