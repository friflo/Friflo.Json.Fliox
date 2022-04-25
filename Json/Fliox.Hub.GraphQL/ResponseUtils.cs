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
        internal static SelectionNode CreateSelection (
            GraphQLField        query,
            IUtf8Buffer         buffer,
            in SelectionObject  objectType)
        {
            return CreateNode(default, query.SelectionSet, buffer, objectType);
        }
        
        private static SelectionNode CreateNode (
            in Utf8String       fieldName,
            GraphQLSelectionSet selectionSet,
            IUtf8Buffer         buffer,
            in SelectionObject  objectType)
        {
            if (selectionSet == null) {
                return new SelectionNode(fieldName, objectType, false, null, null);
            }
            var selections      = selectionSet.Selections;
            var emitTypeName    = ContainsTypename(selections);
            AddSelectionFields(selections, buffer, objectType, out var nodes, out var fragments);
            return new SelectionNode(fieldName, objectType, emitTypeName, nodes, fragments);
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
            List<ASTNode>           selections,
            IUtf8Buffer             buffer,
            in SelectionObject      objectType,
            out SelectionNode[]     nodes,
            out SelectionNode[]     fragments)
        {
            var nodeList        = new List<SelectionNode>();
            var fragmentList    = new List<SelectionNode>();
            foreach (var selection in selections) {
                if      (selection is GraphQLField gqlField) {
                    var fieldName  = gqlField.Name;
                    if (fieldName == "__typename")
                        continue;
                    var fieldNameSpan   = fieldName.Value.Span;
                    var selectionField  = objectType.FindField(fieldNameSpan);
                    var span            = fieldName.Value.Span;
                    var fieldNameUtf8   = buffer.Add(span);
                    var node            = CreateNode(fieldNameUtf8, gqlField.SelectionSet, buffer, selectionField.objectType);
                    nodeList.Add(node);
                }
                else if (selection is GraphQLInlineFragment gqlFragment) {
                    var condition           = gqlFragment.TypeCondition;
                    if (condition == null)
                        continue;
                    var fragmentSelection   = gqlFragment.SelectionSet;
                    var conditionName       = condition.Type.Name;
                    var union               = objectType.FindUnion(conditionName.Value.Span);
                    var fragmentNode        = CreateNode(union.typenameUtf8, fragmentSelection, buffer, union.unionObject);
                    fragmentList.Add(fragmentNode);
                }
            }
            nodes       = nodeList.ToArray();
            fragments   = fragmentList.Count > 0 ? fragmentList.ToArray() : null;
        }
    }
}

#endif
