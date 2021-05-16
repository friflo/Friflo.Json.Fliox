// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Mapper
{
    public class MemberAccess
    {
        internal readonly   PathNodeTree<MemberValue>    nodeTree = new PathNodeTree<MemberValue>();
        internal readonly   List<MemberValue>            results = new List<MemberValue>();
        
        // public           List<ObjectSelectResult>            Results => results;
        
        public MemberAccess(IList<string> selectors) {
            CreateNodeTree(selectors);
            results.Capacity = selectors.Count;
        }
        
        private void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new MemberValue ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init();
            }
        }
    }
    
    // --- Select result ---
    public class MemberValue
    {
        public      string  json;
        public      object  value;

        internal void Init() {
            json    = null;
            value   = null;
        }
    }
}