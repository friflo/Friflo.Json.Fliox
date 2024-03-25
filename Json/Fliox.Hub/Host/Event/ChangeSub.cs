// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    internal readonly struct ChangeSub {
        internal    readonly    ShortString     container;  // never null
        internal    readonly    EntityChange    changes;    // flags
        internal    readonly    JsonFilter      jsonFilter;
        /// <summary>normalized string representation of <see cref="jsonFilter"/> used for comparison</summary>
        internal    readonly    string          filter;
        
        public      override    string          ToString() => GetString();
        
        private string GetString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }
        
        internal void AppendTo(StringBuilder sb) {
            sb.Append('\'');
            container.AppendTo(sb);
            sb.Append('\'');
            sb.Append(": ");
            sb.Append(changes);
            if (filter != null) {
                sb.Append(", filter: ");
                sb.Append(filter);
            }
        } 

        internal ChangeSub(in ShortString container, List<EntityChange> changes, JsonFilter jsonFilter) {
            this.container  = container;
            this.changes    = EntityChangeUtils.ListToFlags(changes);
            this.jsonFilter = jsonFilter;
            filter          = jsonFilter?.Linq;
        }
        
        private bool IsEqual(in ChangeSub other) {
            return container.IsEqual(other.container) &&
                   changes         == other.changes   &&
                   filter          == other.filter;
        }
        
        internal int HashCode() {
            return container.HashCode() ^ (int)changes ^ (filter?.GetHashCode() ?? 0);
        }
        
        internal static bool IsEqual(Dictionary<ShortString, ChangeSub> x, Dictionary<ShortString, ChangeSub> y) {
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