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
        internal    readonly    SmallString     container;
        internal    readonly    EntityChange    changes;    // flags
        internal    readonly    JsonFilter      jsonFilter;
        /// <summary>normalized string representation of <see cref="jsonFilter"/> used for comparison</summary>
        internal    readonly    string          filter;
        
        public      override    string          ToString() => GetString();
        
        internal static readonly  ChangeSubComparer Equality = new ChangeSubComparer();

        
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
        
        internal int HashCode() {
            return container.value.GetHashCode() ^ (int)changes ^ filter.GetHashCode();
        }
    }
    
    internal sealed class ChangeSubComparer : IEqualityComparer<ChangeSub>
    {
        public bool Equals(ChangeSub x, ChangeSub y) {
            return x.container.value == y.container.value &&
                   x.changes         == y.changes         &&
                   x.filter          == y.filter;
        }

        public int GetHashCode(ChangeSub value) {
            return value.HashCode();
        }
    }
}