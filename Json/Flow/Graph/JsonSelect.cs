// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph
{

    

    public class JsonSelect
    {
        internal readonly   PathNodeTree<ScalarResult>    nodeTree = new PathNodeTree<ScalarResult>();
        internal readonly   List<ScalarResult>            results = new List<ScalarResult>();
        
        public              List<ScalarResult>            Results => results;

        
        internal JsonSelect() { }
        
        public JsonSelect(string selector) {
            var selectors = new[] {selector};
            CreateNodeTree(selectors);
            results.Capacity = selectors.Length;
        }
        
        public JsonSelect(IList<string> selectors) {
            CreateNodeTree(selectors);
            results.Capacity = selectors.Count;
        }

        internal void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new ScalarResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init();
            }
        }
    }
}