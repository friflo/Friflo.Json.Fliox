// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Flow.Graph.Select
{
    public class ScalarResult
    {
        public   readonly   List<Scalar>    values          = new List<Scalar>();
        public   readonly   List<int>       groupIndices    = new List<int>();
        private             int             lastGroupIndex;

        internal void Init() {
            values.Clear();
            groupIndices.Clear();
            lastGroupIndex = -1;
        }

        internal static void Add(Scalar scalar, List<PathSelector<ScalarResult>> selectors) {
            foreach (var selector in selectors) {
                var parentGroup = selector.parentGroup;
                var result = selector.result;
                if (parentGroup != null) {
                    var index = parentGroup.arrayIndex;
                    if (index != result.lastGroupIndex) {
                        result.lastGroupIndex = index;
                        result.groupIndices.Add(result.values.Count);
                    }
                }
                result.values.Add(scalar);
            }
        }

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
}