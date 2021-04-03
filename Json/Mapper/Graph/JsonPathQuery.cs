// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResult  {
        internal readonly   StringBuilder   arrayResult;
        internal            string          jsonResult;
        internal            int             itemCount;

        internal SelectorResult(StringBuilder arrayResult) {
            this.arrayResult    = arrayResult;
        }
    }
    
    public class JsonPathQuery : PathSelector<SelectorResult>
    {
        internal JsonPathQuery() { }
        
        public JsonPathQuery(IList<string> pathList) {
            CreateSelector(pathList);
        }

        public IList<string> GetResult() {
            var result = leafNodes.Select(leaf => {
                var arrayResult = leaf.node.result.arrayResult;
                if (arrayResult != null) {
                    arrayResult.Append(']');
                    return arrayResult.ToString();
                }
                return leaf.node.result.jsonResult;
            }).ToList();
            return result;
        }

        internal new void CreateSelector(IList<string> pathList) {
            base.CreateSelector(pathList);
            foreach (var leaf in leafNodes) {
                StringBuilder arrayResult = leaf.isArrayResult ? new StringBuilder() : null;
                leaf.node.result = new SelectorResult (arrayResult);
            }
        }
        
        internal void InitSelector() {
            foreach (var leaf in leafNodes) {
                var sb = leaf.node.result.arrayResult;
                if (sb != null) {
                    sb.Clear();
                    sb.Append('[');
                }
                leaf.node.result.itemCount = 0;
                leaf.node.result.jsonResult = null;
            }
        }
    }
}