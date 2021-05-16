// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Select;

namespace Friflo.Json.Flow.Mapper
{
    public class ObjectSelector : IDisposable
    {
        private readonly MemberSelector selector;


        public ObjectSelector(TypeStore typeStore) {
            selector = new MemberSelector(typeStore);
        }
        
        public void Dispose() {
            selector.Dispose();
        }
        
        public List<ObjectSelectResult> Select<T>(T value, ObjectSelect select) {
            select.InitSelectorResults();
            var mapper = selector.TypeCache.GetTypeMapper(typeof(T));
            var rootNode = select.nodeTree.rootNode;
            mapper.MemberObject(selector, value, rootNode);
            
            // refill result list cause application code may mutate between Select() calls
            var results = select.results; 
            results.Clear();
            foreach (var sel in select.nodeTree.selectors) {
                results.Add(sel.result);
            }
            return results;
        }
    }
}
