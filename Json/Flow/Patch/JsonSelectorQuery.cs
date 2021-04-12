// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Patch
{
    public class SelectorResult
    {
        public  readonly    List<Scalar>    values = new List<Scalar>();

        internal SelectorResult() { }

        public List<string> AsStringList() {
            var result = new List<string>(values.Count);
            foreach (var item in values) {
                result.Add(item.AsString());
            }
            return result;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AppendItemAsString(sb);
            return sb.ToString();
        }

        /// Format as debug string - not as JSON 
        private void AppendItemAsString(StringBuilder sb) {
            switch (values.Count) {
                case 0:
                    sb.Append("[]");
                    break;
                case 1:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    sb.Append(']');
                    break;
                default:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    for (int n = 1; n < values.Count; n++) {
                        sb.Append(',');
                        values[n].AppendTo(sb);
                    }
                    sb.Append(']');
                    break;
            }
        }
    }
    

    public class JsonSelectorQuery : PathNodeTree<SelectorResult>
    {
        // public because used by JsonLambda in a separate library
        public JsonSelectorQuery() { }
        
        public JsonSelectorQuery(IList<string> pathList) {
            CreateNodeTree(pathList);
        }

        // public because used by JsonLambda in a separate library
        public new void CreateNodeTree(IList<string> pathList) {
            base.CreateNodeTree(pathList);
            foreach (var leaf in leafNodes) {
                leaf.node.result = new SelectorResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var leaf in leafNodes) {
                leaf.node.result.values.Clear();
            }
        }

        public List<SelectorResult> GetResult() {
            var results = new List<SelectorResult>(leafNodes.Count);
            foreach (var leaf in leafNodes) {
                results.Add(leaf.node.result);
            }
            return results;
        }
    }
}