// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Transform
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonSelect
    {
        internal readonly   PathNodeTree<JsonSelectResult>    nodeTree = new PathNodeTree<JsonSelectResult>();
        internal readonly   List<JsonSelectResult>            results = new List<JsonSelectResult>();
        
        public              List<JsonSelectResult>            Results => results;

        
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

        private void CreateNodeTree(IList<string> pathList) {
            nodeTree.CreateNodeTree(pathList);
            foreach (var selector in nodeTree.selectors) {
                selector.result = new JsonSelectResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in nodeTree.selectors) {
                selector.result.Init();
            }
        }
    }
    
    // --- Select result ---
    public class JsonSelectResult
    {
        public   readonly   List<string>    values          = new List<string>();


        internal void Init() {
            values.Clear();
        }

        internal static void Add(string scalar, List<PathSelector<JsonSelectResult>> selectors) {
            foreach (var selector in selectors) {
                var result = selector.result;
                result.values.Add(scalar);
            }
        }
    }
}