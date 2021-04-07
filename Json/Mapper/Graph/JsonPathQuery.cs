// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResults
    {
        internal    readonly    List<SelectorResult>    items = new List<SelectorResult>();

        public List<string> AsStringList() {
            var result = new List<string>(items.Count);
            foreach (var item in items) {
                result.Add(item.value);
            }
            return result;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AppendAsJson(sb);
            return sb.ToString();
        }

        private void AppendAsJson(StringBuilder sb) {
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
        }
    }

    public class SelectorResult
    {
        internal    readonly    ResultType      type;
        internal    readonly    string          value;
        internal    readonly    double          doubleValue;
        internal    readonly    long            longValue;
        internal    readonly    bool            boolValue;
        

        public SelectorResult(ResultType type, string value) {
            this.type   = type;
            this.value  = value;
        }
        
        public SelectorResult(double value) {
            type        = ResultType.Double;
            doubleValue = value;
        }
        
        public SelectorResult(long value) {
            type        = ResultType.Long;
            longValue   = value;
        }

        public SelectorResult(bool boolValue) {
            type            = ResultType.Bool;
            this.boolValue  = boolValue;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        public void AppendTo(StringBuilder sb) {
            switch (type) {
                case ResultType.Array:
                case ResultType.Object:
                    sb.Append(value);
                    break;
                case ResultType.Double:
                    sb.Append(doubleValue);
                    break;
                case ResultType.Long:
                    sb.Append(longValue);
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
        Double,
        Long,
        Bool,
        Null
    }
    
    public class JsonPathQuery : PathNodeTree<SelectorResults>
    {
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

        public List<SelectorResults> GetResult() {
            var results = new List<SelectorResults>(leafNodes.Count);
            foreach (var leaf in leafNodes) {
                results.Add(leaf.node.result);
            }
            return results;
        }
    }
}