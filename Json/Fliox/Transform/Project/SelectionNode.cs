// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    public readonly struct SelectionNode
    {
        private   readonly      SelectionNode[]     nodes;
        private   readonly      Utf8String          fieldName;
        internal  readonly      bool                emitTypeName;
        internal  readonly      SelectionUnion[]    unions;
        internal  readonly      Utf8String          typeName;
        internal  readonly      bool                allFields;

        public override         string              ToString() => FormatToString();

        public SelectionNode  (
            in Utf8String       fieldName,
            in SelectionObject  objectType,
            bool                emitTypeName,
            SelectionNode[]     nodes,
            bool                allFields)
        {
            this.fieldName      = fieldName;
            this.typeName       = objectType.name;
            this.emitTypeName   = emitTypeName;
            this.unions         = objectType.unions;
            this.nodes          = nodes;
            this.allFields      = allFields;
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
                if (!node.fieldName.IsEqual(ref key))
                    continue;
                result = node;
                return true;
            }
            result = default;
            return false;
        }
        
                
        public Utf8String FindUnionType (ref Bytes discriminant) {
            if (unions == null) {
                return default;
            }
            foreach (var union in unions) {
                if (!union.discriminant.IsEqual(ref discriminant))
                    continue;
                return union.typename;
            }
            return default;
        }
    }
}
