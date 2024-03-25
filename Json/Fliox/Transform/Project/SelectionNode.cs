// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    public readonly struct SelectionNode
    {
        private   readonly      SelectionNode[]     nodes;
        private   readonly      Utf8String          fieldName;
        internal  readonly      bool                emitTypeName;
        internal  readonly      SelectionUnion[]    unions;     // can be null
        internal  readonly      Utf8String          typeName;
        internal  readonly      SelectionNode[]     fragments;  // can be null
        
        internal                bool                HasNodes    => nodes != null;
        public override         string              ToString()  => FormatToString();

        public SelectionNode  (
            in Utf8String       fieldName,
            in SelectionObject  objectType,
            bool                emitTypeName,
            SelectionNode[]     nodes,
            SelectionNode[]     fragments)
        {
            this.fieldName      = fieldName;
            this.typeName       = objectType.name;
            this.emitTypeName   = emitTypeName;
            this.unions         = objectType.unions;
            this.nodes          = nodes;
            this.fragments      = fragments;
        }
        
        private string FormatToString() {
            var selectionName = fieldName.IsNull ? "(root)" : fieldName.ToString();
            if (nodes == null)
                return selectionName;
            return $"{selectionName} - nodes: {nodes.Length}";
        }
        
        public bool FindField(in Bytes key, out SelectionNode result) {
            for (int n = 0; n < nodes.Length; n++) {
                var node  = nodes[n];
                if (!node.fieldName.IsEqual(key))
                    continue;
                result = node;
                return true;
            }
            result = default;
            return false;
        }
        
        public static bool FindFragment(SelectionNode[] fragmentNodes, in Bytes key, out SelectionNode result) {
            for (int n = 0; n < fragmentNodes.Length; n++) {
                var node  = fragmentNodes[n];
                if (!node.fieldName.IsEqual(key))
                    continue;
                result = node;
                return true;
            }
            result = default;
            return false;
        }

        public Utf8String FindUnionType (in Bytes discriminant) {
            if (unions == null) {
                return default;
            }
            foreach (var union in unions) {
                if (!union.discriminant.IsEqual(discriminant))
                    continue;
                return union.typenameUtf8;
            }
            return default;
        }
        
        public SelectionNode[] FindFragmentNodes (in Utf8String typename) {
            if (fragments == null) {
                return null;
            }
            foreach (var fragment in fragments) {
                if (!fragment.typeName.IsEqual(typename))
                    continue;
                return fragment.nodes;
            }
            return null;
        }
    }
}
