// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Transform.Project;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class ResponseUtils
    {
        internal static SelectionNode CreateSelection (GraphQLField query, IUtf8Buffer buffer, in SelectionObject objectType) {
            return CreateNode(null, query.SelectionSet, buffer, objectType);
        }
        
        private static SelectionNode CreateNode (
            GraphQLName         fieldName,
            GraphQLSelectionSet selectionSet,
            IUtf8Buffer         buffer,
            in SelectionObject  objectType)
        {
            Utf8String fieldNameUtf8;
            if (fieldName == (object)null) {
                fieldNameUtf8   = default;
            } else {
                var span        = fieldName.Value.Span;
                fieldNameUtf8   = buffer.Add(span);
            }
            if (selectionSet == null) {
                return new SelectionNode(fieldNameUtf8, objectType, false, null);
            }
            var selections      = selectionSet.Selections;
            var count           = selections.Count;
            var emitTypeName    = ContainsTypename(selections);
            if (emitTypeName)   { count--; }
            var nodes   = new SelectionNode[count];
            AddSelectionFields(nodes, selections, buffer, objectType);
            return new SelectionNode(fieldNameUtf8, objectType, emitTypeName, nodes);
        }
        
        private static bool ContainsTypename(List<ASTNode> selections) {
            foreach (var selection in selections) {
                var gqlField    = (GraphQLField)selection;
                if (gqlField.Name == "__typename")
                    return true;
            }
            return false;
        }
        
        private static void AddSelectionFields(
            SelectionNode[]     nodes,
            List<ASTNode>       selections,
            IUtf8Buffer         buffer,
            in SelectionObject  objectType)
        {
            int i = 0;
            foreach (var selection in selections) {
                var gqlField   = (GraphQLField)selection;
                var fieldName  = gqlField.Name;
                if (fieldName == "__typename")
                    continue;
                var fieldNameSpan   = fieldName.Value.Span;
                var selectionField  = objectType.FindField(fieldNameSpan);
                nodes[i++]          = CreateNode(fieldName, gqlField.SelectionSet, buffer, selectionField.objectType);
            }
        }
    }
}

#endif
