// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using GraphQLParser;
using GraphQLParser.AST;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct SelectionNode
    {
        internal  readonly      SelectionNode[] nodes;
        internal  readonly      ROM             name;

        public override         string          ToString() => name.ToString();

        internal SelectionNode (in Query query)
            : this (null, query.graphQL.SelectionSet)
        { }
            
        private SelectionNode  (GraphQLName name, GraphQLSelectionSet selectionSet) {
            this.name = name;
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
    }
}

#endif