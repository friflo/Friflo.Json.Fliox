// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResults
    {
        internal    readonly    List<SelectorResult>    items = new List<SelectorResult>();
    }

    public class SelectorResult
    {
        internal    readonly    ResultType      type;
        internal    readonly    string          value;
        internal    readonly    bool            boolValue;
        

        public SelectorResult(ResultType type, string value) {
            this.type   = type;
            this.value  = value;
        }
        
        public SelectorResult(bool boolValue) {
            type            = ResultType.Bool;
            value           = boolValue ? "true" : "false";
            this.boolValue  = boolValue;
        }

        public void AppendTo(StringBuilder sb) {
            switch (type) {
                case ResultType.Array:
                case ResultType.Object:
                case ResultType.Number:
                    sb.Append(value);
                    break;
                case ResultType.String:
                    sb.Append('"');
                    sb.Append(value);
                    sb.Append('"');
                    break;
                case ResultType.Bool:
                    sb.Append(boolValue ? "true": "false");
                    break;
                case ResultType.Null:
                    sb.Append("null");
                    break;
            }
        }
    }

    public enum ResultType {
        Array,
        Object,
        String,
        Number,
        Bool,
        Null
    }
    
    public class JsonPathQuery : PathNodeTree<SelectorResults>
    {
        private    readonly    StringBuilder           sb = new StringBuilder();

        internal JsonPathQuery() { }
        
        public JsonPathQuery(IList<string> pathList) {
            CreateNodeTree(pathList);
        }

        internal new void CreateNodeTree(IList<string> pathList) {
            base.CreateNodeTree(pathList);
            foreach (var leaf in leafNodes) {
                leaf.node.result = new SelectorResults ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var leaf in leafNodes) {
                leaf.node.result.items.Clear();
            }
        }
        
        public IList<string> GetResult() {
            var results = new List<string>(leafNodes.Count);
            foreach (var leaf in leafNodes) {
                sb.Clear();
                var items = leaf.node.result.items;
                switch (items.Count) {
                    case 0:
                        sb.Append("[]");
                        break;
                    case 1:
                        sb.Append('[');
                        items[0].AppendTo(sb);
                        sb.Append(']');
                        break;
                    default:
                        sb.Append('[');
                        items[0].AppendTo(sb);
                        for (int n = 1; n < items.Count; n++) {
                            sb.Append(',');
                            items[n].AppendTo(sb);
                        }
                        sb.Append(']');
                        break;
                }
                var value = sb.ToString();
                results.Add(value);
            }
            sb.Clear();
            return results;
        }
    }
}