// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
        /// <summary>
        /// In contrast to <see cref="typeStore"/> this Cache is by intention not thread safe.
        /// It is created within a <see cref="JsonReader"/> and <see cref="JsonWriter"/> to access type information
        /// without locking if already cached.
        /// </summary>
#if !UNITY_5_3_OR_NEWER
        [CLSCompliant(true)]
#endif
        public class TypeCache : IDisposable
        {
            private readonly    Dictionary <Type,  TypeMapper>  typeMap =      new Dictionary <Type,  TypeMapper >();
            //
            private readonly    Dictionary <Bytes, TypeMapper>  nameToType =   new Dictionary <Bytes, TypeMapper >();
            private readonly    Dictionary <Type,  Bytes>       typeToName =   new Dictionary <Type,  Bytes >();
            
            private readonly    TypeStore                       typeStore;
            private             int                             lookupCount;

            public              int     LookupCount         =>  lookupCount;
            public              int     StoreLookupCount    =>  typeStore.storeLookupCount;
            public              int     TypeCreationCount   =>  typeStore.typeCreationCount;

            
            public TypeCache (TypeStore typeStore) {
                this.typeStore = typeStore;
            }
            
            public void Dispose() {
                foreach (var item in nameToType.Keys)
                    item.Dispose();
                foreach (var item in typeToName.Values)
                    item.Dispose();
            }
            
            public TypeMapper GetTypeMapper (Type type) {
                lookupCount++;
                if (!typeMap.TryGetValue(type, out TypeMapper propType)) {
                    propType = typeStore.GetTypeMapper(type);
                    typeMap.Add(type, propType);
                }
                return propType;
            }

            public void ClearCounts() {
                lookupCount = 0;
                typeStore.storeLookupCount = 0;
                typeStore.typeCreationCount = 0;
            }

            /// <summary>
            /// Lookup by Type discriminator registered initially with <see cref="TypeStore.RegisterType"/>  
            /// </summary>
            public TypeMapper GetTypeByName(ref Bytes name) {
                if (!nameToType.TryGetValue(name, out TypeMapper propType)) {
                    lock (typeStore) {
                        typeStore.nameToType.TryGetValue(name, out TypeMapper storeType);
                        propType = storeType;
                    }
                    if (propType == null)
                        throw new InvalidOperationException("No type registered with discriminator: " + name);
                    
                    Bytes newName = new Bytes(ref name);
                    nameToType.Add(newName, propType);
                }
                return propType;
            }

            /// <summary>
            /// Append the Type discriminator registered initially with <see cref="TypeStore.RegisterType"/>  
            /// </summary>
            public void AppendDiscriminator(ref Bytes dst, TypeMapper type) {
                Type nativeType = type.type;
                typeToName.TryGetValue(nativeType, out Bytes name);
                if (!name.buffer.IsCreated()) {
                    lock (typeStore) {
                        typeStore.typeToName.TryGetValue(nativeType, out Bytes storeName);
                        name = storeName;
                    }
                    if (!name.buffer.IsCreated())
                        throw new InvalidOperationException("no discriminator registered for type: " + nativeType);
                    
                    Bytes newName = new Bytes(ref name);
                    typeToName.Add(nativeType, newName);
                }
                dst.AppendBytes(ref name);
            }
        }
}