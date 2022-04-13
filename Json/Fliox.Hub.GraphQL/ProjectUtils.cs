// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Project;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ProjectUtils
    {
        internal static SelectionNode CreateSelection (in Query query, IUtf8Buffer buffer) {
            return CreateNode(null, query.graphQL.SelectionSet, buffer);
        }
        
        private static SelectionNode CreateNode (GraphQLName name, GraphQLSelectionSet selectionSet, IUtf8Buffer buffer) {
            Utf8String nameUtf8;
            if (name == (object)null) {
                nameUtf8            = new Utf8String();
            } else {
                var readOnlySpan    = name.Value.Span;
                nameUtf8            = buffer.Add(readOnlySpan);
            }
            SelectionNode[] nodes;
            if (selectionSet == null) {
                nodes = null;
            } else {
                var selections  = selectionSet.Selections;
                nodes = new SelectionNode[selections.Count];
                for (int n = 0; n < selections.Count; n++) {
                    var selection   = (GraphQLField)selections[n];
                    nodes[n]        = CreateNode(selection.Name, selection.SelectionSet, buffer);
                }
            }
            return new SelectionNode(nameUtf8, nodes);
        }
    }
}

#endif
