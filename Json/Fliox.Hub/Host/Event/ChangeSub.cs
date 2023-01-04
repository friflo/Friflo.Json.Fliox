// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal readonly struct ChangeSub {
        internal    readonly    SmallString     container;  // never null
        internal    readonly    EntityChange    changes;    // flags
        internal    readonly    JsonFilter      jsonFilter;
        /// <summary>normalized string representation of <see cref="jsonFilter"/> used for comparison</summary>
        internal    readonly    string          filter;
        
        public      override    string          ToString() => GetString();
        
        private string GetString() {
            var sb = new StringBuilder();
            sb.Append(container.value);
            sb.Append(": ");
            sb.Append(changes);
            if (filter != null) {
                sb.Append(", filter: ");
                sb.Append(filter);
            }
            return sb.ToString();
        } 

        internal ChangeSub(string container, List<EntityChange> changes, JsonFilter jsonFilter) {
            this.container  = new SmallString(container);
            this.changes    = EntityChangeUtils.ListToFlags(changes);
            this.jsonFilter = jsonFilter;
            filter          = jsonFilter?.Linq;
        }
        
        private bool IsEqual(in ChangeSub other) {
            return container.value == other.container.value &&
                   changes         == other.changes         &&
                   filter          == other.filter;
        }
        
        internal int HashCode() {
            return container.value.GetHashCode() ^ (int)changes ^ (filter?.GetHashCode() ?? 0);
        }
        
        internal static bool IsEqual(Dictionary<string, ChangeSub> x, Dictionary<string, ChangeSub> y) {
            if (x.Count != y.Count) {
                return false;
            }
            foreach (var xPair in x) {
                if (!y.TryGetValue(xPair.Key, out var yItem)) {
                    return false;
                }
                if (!xPair.Value.IsEqual(yItem)) {
                    return false;
                }
            }
            return true;
        }
    }
}