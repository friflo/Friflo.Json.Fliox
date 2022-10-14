// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper
{
    public class ObjectTools
    {
        private readonly TypeCache typeCache;
        
        public ObjectTools(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
        }
        
        public void DeepCopy<T>(T from, ref T target) {
            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            mapper.Copy(from, ref target);
        }
        
        public T Clone<T>(T value) where T : new() {
            var mapper  = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var clone   = new T();
            mapper.Copy(value, ref clone);
            return clone;
        }
    }
}