// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Mapper
{
    public class ObjectSelect
    {
        internal readonly   PathNodeTree<ObjectSelectResult>    nodeTree = new PathNodeTree<ObjectSelectResult>();
        internal readonly   List<ObjectSelectResult>            results = new List<ObjectSelectResult>();
        
        // public           List<ObjectSelectResult>            Results => results;
        
        public ObjectSelect(IList<string> selectors) {
            CreateNodeTree(selectors);
            results.Capacity = selectors.Count;
        }
        
        private void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new ObjectSelectResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init();
            }
        }
    }
    
    // --- Select result ---
    public class ObjectSelectResult
    {
        public      string  json;
        public      object  value;

        internal void Init() {
            json    = null;
            value   = null;
        }
    }
}