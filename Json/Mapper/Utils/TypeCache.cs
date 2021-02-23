// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Utils
{
    /// <summary>
    /// In contrast to <see cref="typeStore"/> this Cache is by intention not thread safe.
    /// It is created within a <see cref="Reader"/> and <see cref="JsonWriter"/> to access type information
    /// without locking if already cached.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeCache
    {
        private readonly    Dictionary <Type,  TypeMapper>  typeMap =      new Dictionary <Type,  TypeMapper >();
        
        private readonly    TypeStore                       typeStore;
        private             int                             lookupCount;

        public              int     LookupCount         =>  lookupCount;
        public              int     StoreLookupCount    =>  typeStore.storeLookupCount;
        public              int     TypeCreationCount   =>  typeStore.typeCreationCount;

        
        internal TypeCache (TypeStore typeStore) {
            this.typeStore = typeStore;
        }
        
        internal void Dispose() {
        }
        
        internal TypeMapper GetTypeMapper (Type type) {
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
    }
}