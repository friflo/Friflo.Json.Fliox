// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Access;

namespace Friflo.Json.Flow.Mapper
{
    public class MemberAccessor : IDisposable
    {
        private readonly Accessor accessor;


        public MemberAccessor(TypeStore typeStore) {
            accessor = new Accessor(typeStore);
        }
        
        public MemberAccessor(ObjectWriter writer) {
            accessor = new Accessor(writer);
        }
        
        public void Dispose() {
            accessor.Dispose();
        }
        
        public IReadOnlyList<MemberValue> GetValues<T>(T value, MemberAccess access) {
            access.InitSelectorResults();
            var mapper = accessor.TypeCache.GetTypeMapper(typeof(T));
            var rootNode = access.nodeTree.rootNode;
            mapper.MemberObject(accessor, value, rootNode);
            
            // refill result list cause application code may mutate between Select() calls
            var results = access.results; 
            results.Clear();
            foreach (var sel in access.nodeTree.selectors) {
                results.Add(sel.result);
            }
            return results;
        }
    }
}
