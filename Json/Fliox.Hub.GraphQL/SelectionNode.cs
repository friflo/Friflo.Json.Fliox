// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct SelectionNode
    {
        internal  readonly      SelectionNode[] nodes;
        internal  readonly      JsonValue       name;

        public override         string          ToString() => name.ToString();

        internal SelectionNode (in Query query)
            : this (null, query.graphQL.SelectionSet)
        { }
        
        private static readonly UTF8Encoding Utf8    = new UTF8Encoding(false);
            
        private SelectionNode  (GraphQLName name, GraphQLSelectionSet selectionSet) {
            if (name != (object)null) {
                var         readOnlySpan    = name.Value.Span;
                var         len             = Utf8.GetByteCount(readOnlySpan);
                var         bytes           = new byte[len];
                Span<byte>  dest            = bytes;
                Utf8.GetBytes(readOnlySpan, dest);
                this.name                   = new JsonValue(bytes);
            }
            if (selectionSet == null) {
                nodes = null;
                return;
            }
            var selections  = selectionSet.Selections;
            nodes           = new SelectionNode[selections.Count];
            for (int n = 0; n < selections.Count; n++) {
                var selection   = (GraphQLField)selections[n];
                nodes[n]        = new SelectionNode(selection.Name, selection.SelectionSet);
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