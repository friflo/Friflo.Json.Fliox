// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResult
    {
        internal    readonly    List<SelectorValue>    values = new List<SelectorValue>();

        public List<string> AsStringList() {
            var result = new List<string>(values.Count);
            foreach (var item in values) {
                result.Add(item.stringValue);
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

    /// Note: Could be a readonly struct, but performance degrades and API gets unhandy if so.
    public class SelectorValue
    {
        internal    readonly    ResultType      type;
        internal    readonly    string          stringValue;
        internal    readonly    double          doubleValue;
        internal    readonly    long            longValue;
        internal    readonly    bool            boolValue;
        

        public SelectorValue(ResultType type, string value) {
            this.type   = type;
            stringValue = value;
            doubleValue = 0;
            longValue   = 0;
            boolValue   = false;
        }
        
        public SelectorValue(double value) {
            type        = ResultType.Double;
            doubleValue = value;
            stringValue = null;
            longValue   = 0;
            boolValue   = false;
        }
        
        public SelectorValue(long value) {
            type        = ResultType.Long;
            longValue   = value;
            stringValue = null;
            doubleValue = 0;
            boolValue   = false;
        }

        public SelectorValue(bool value) {
            type        = ResultType.Bool;
            boolValue   = value;
            stringValue = null;
            doubleValue = 0;
            longValue   = 0;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        /// Format as debug string - not as JSON
        internal void AppendTo(StringBuilder sb) {
            switch (type) {
                case ResultType.Array:
                case ResultType.Object:
                    sb.Append(stringValue);
                    break;
                case ResultType.Double:
                    sb.Append(doubleValue);
                    break;
                case ResultType.Long:
                    sb.Append(longValue);
                    break;
                case ResultType.String:
                    sb.Append('\'');
                    sb.Append(stringValue);
                    sb.Append('\'');
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
    
    public class JsonSelectorQuery : PathNodeTree<SelectorResult>
    {
        internal JsonSelectorQuery() { }
        
        public JsonSelectorQuery(IList<string> pathList) {
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