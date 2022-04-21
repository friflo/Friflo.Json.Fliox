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
            GraphQLName         name,
            GraphQLSelectionSet selectionSet,
            IUtf8Buffer         buffer,
            in SelectionObject  objectType)
        {
            Utf8String nameUtf8;
            if (name == (object)null) {
                nameUtf8    = default;
            } else {
                var span    = name.Value.Span;
                nameUtf8    = buffer.Add(span);
            }
            if (selectionSet == null) {
                return new SelectionNode(nameUtf8, objectType.name, false, null);
            }
            var selections      = selectionSet.Selections;
            var count           = selections.Count;
            var emitTypeName    = ContainsTypename(selections);
            if (emitTypeName)   count--;
            var nodes   = new SelectionNode[count];
            var i       = 0;
            foreach (var selection in selections) {
                var gqlField   = (GraphQLField)selection;
                var fieldName  = gqlField.Name;
                if (fieldName == "__typename")
                    continue;
                var fieldNameSpan   = fieldName.Value.Span;
                var selectionField  = objectType.FindField(fieldNameSpan);
                nodes[i++]          = CreateNode(fieldName, gqlField.SelectionSet, buffer, selectionField.objectType);
            }
            return new SelectionNode(nameUtf8, objectType.name, emitTypeName, nodes);
        }
        
        private static bool ContainsTypename(List<ASTNode> selections) {
            foreach (var selection in selections) {
                var gqlField    = (GraphQLField)selection;
                if (gqlField.Name == "__typename")
                    return true;
            }
            return false;
        }
    }
}

#endif
