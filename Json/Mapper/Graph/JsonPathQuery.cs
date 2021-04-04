// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResult
    {
        internal readonly   StringBuilder   arrayResult = new StringBuilder();
        internal            int             itemCount;
    }
    
    public class JsonPathQuery : PathNodeTree<SelectorResult>
    {
        internal JsonPathQuery() { }
        
        public JsonPathQuery(IList<string> pathList) {
            CreateNodeTree(pathList);
        }

        internal new void CreateNodeTree(IList<string> pathList) {
            base.CreateNodeTree(pathList);
            foreach (var leaf in leafNodes) {
                leaf.node.result = new SelectorResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var leaf in leafNodes) {
                var sb = leaf.node.result.arrayResult;
                sb.Clear();
                sb.Append('[');
                leaf.node.result.itemCount = 0;
            }
        }
        
        public IList<string> GetResult() {
            var result = leafNodes.Select(leaf => {
                var arrayResult = leaf.node.result.arrayResult;
                arrayResult.Append(']');
                return arrayResult.ToString();
            }).ToList();
            return result;
        }
    }
}