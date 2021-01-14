// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
{
        /// <summary>
        /// In contrast to <see cref="typeStore"/> this Cache is by intention not thread safe.
        /// It is created within a <see cref="JsonReader"/> and <see cref="JsonWriter"/> to access type information
        /// without locking if already cached.
        /// </summary>
        public class TypeCache
        {
            private readonly    HashMapLang <Type,  StubType>   typeMap =      new HashMapLang <Type,  StubType >();
            //
            private readonly    HashMapLang <Bytes, StubType>   nameToType =   new HashMapLang <Bytes, StubType >();
            private readonly    HashMapLang <Type, Bytes>       typeToName =   new HashMapLang <Type,  Bytes >();
            
            private readonly    TypeStore                       typeStore;
            private             int                            lookupCount;

            public              int     LookupCount         => lookupCount;
            public              int     StoreLookupCount    => typeStore.storeLookupCount;
            public              int     TypeCreationCount   => typeStore.typeCreationCount;
            


            public TypeCache (TypeStore typeStore) {
                this.typeStore = typeStore;
            }
            
            public StubType GetType (Type type) {
                lookupCount++;
                StubType propType = typeMap.Get(type);
                if (propType == null) {
                    propType = typeStore.GetType(type);
                    typeMap.Put(type, propType);
                }
                return propType;
            }

            public void ClearCounts() {
                lookupCount = 0;
                typeStore.storeLookupCount = 0;
                typeStore.typeCreationCount = 0;
            }

            public StubType GetTypeByName(ref Bytes name) {
                StubType propType = nameToType.Get(name);
                if (propType == null) {
                    lock (typeStore) {
                        propType = typeStore.nameToType.Get(name);
                    }
                }
                if (propType == null)
                    throw new InvalidOperationException("No type registered with discriminator: " + name);
                return propType;
            }

            public void AppendDiscriminator(ref Bytes dst, StubType type) {
                Bytes name = typeToName.Get(type.type);
                if (!name.buffer.IsCreated()) {
                    lock (typeStore) {
                        name = typeStore.typeToName.Get(type.type);
                    }
                }
                if (!name.buffer.IsCreated())
                    throw new InvalidOperationException("no discriminator registered for type: " + type.type.FullName);
                dst.AppendBytes(ref name);
            }
        }
}