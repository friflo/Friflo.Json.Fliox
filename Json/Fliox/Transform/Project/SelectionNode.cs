// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    public readonly struct SelectionNode
    {
        private   readonly      SelectionNode[] nodes;
        private   readonly      Utf8String      name;

        public override         string          ToString() => FormatToString();

        public SelectionNode  (in Utf8String name, SelectionNode[] nodes) {
            this.name           = name;
            this.nodes          = nodes;
        }
        
        private string FormatToString() {
            var selectionName = name.IsNull ? "(root)" : name.ToString();
            if (nodes == null)
                return selectionName;
            return $"{selectionName} - nodes: {nodes.Length}";
        }
        
        public bool FindByBytes(ref Bytes key, out SelectionNode result) {
            for (int n = 0; n < nodes.Length; n++) {
                var node  = nodes[n];
                if (node.name.IsEqual(ref key)) {
                    result = node;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}

#endif