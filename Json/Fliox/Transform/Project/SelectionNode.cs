// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    public readonly struct SelectionNode
    {
        private   readonly      SelectionNode[] nodes;
        private   readonly      Utf8String      fieldName;
        internal  readonly      bool            emitTypeName;
        internal  readonly      Utf8String      typeName;

        public override         string          ToString() => FormatToString();

        public SelectionNode  (in Utf8String fieldName, in Utf8String typeName, bool emitTypeName, SelectionNode[] nodes) {
            this.fieldName      = fieldName;
            this.typeName       = typeName;
            this.emitTypeName   = emitTypeName;
            this.nodes          = nodes;
        }
        
        private string FormatToString() {
            var selectionName = fieldName.IsNull ? "(root)" : fieldName.ToString();
            if (nodes == null)
                return selectionName;
            return $"{selectionName} - nodes: {nodes.Length}";
        }
        
        public bool FindField(ref Bytes key, out SelectionNode result) {
            for (int n = 0; n < nodes.Length; n++) {
                var node  = nodes[n];
                if (node.fieldName.IsEqual(ref key)) {
                    result = node;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
