// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    public sealed class ObjectTools
    {
        private readonly TypeCache typeCache;
        
        public ObjectTools(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
        }
        
        public void DeepCopy<T>(T src, ref T dst) {
            if (src == null) {
                dst = default;
                return;
            }
            if (dst == null) {
                dst = default;
            }
            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            mapper.Copy(src, ref dst);
        }
        
        public T Clone<T>(T value) where T : new() {
            if (value == null) {
                return default;
            }
            var mapper  = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var clone   = new T();
            mapper.Copy(value, ref clone);
            return clone;
        }
    }
}