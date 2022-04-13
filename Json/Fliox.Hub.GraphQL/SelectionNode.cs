// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Burst;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct SelectionNode
    {
        private   readonly      SelectionNode[] nodes;
        private   readonly      Utf8String      name;

        public override         string          ToString() => name.ToString();

        internal SelectionNode (in Query query, Utf8Buffer buffer)
            : this (null, query.graphQL.SelectionSet, buffer)
        { }
            
        private SelectionNode  (GraphQLName name, GraphQLSelectionSet selectionSet, Utf8Buffer buffer) {
            if (name == (object)null) {
                this.name           = new Utf8String();
            } else {
                var readOnlySpan    = name.Value.Span;
                this.name           = buffer.Add(readOnlySpan);
            }
            if (selectionSet == null) {
                nodes       = null;
                return;
            }
            var selections  = selectionSet.Selections;
            nodes           = new SelectionNode[selections.Count];
            for (int n = 0; n < selections.Count; n++) {
                var selection   = (GraphQLField)selections[n];
                nodes[n]        = new SelectionNode(selection.Name, selection.SelectionSet, buffer);
            }
        }
        
        internal bool FindByBytes(ref Bytes key, out SelectionNode result) {
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