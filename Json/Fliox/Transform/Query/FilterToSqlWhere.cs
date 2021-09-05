// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query
{
    internal static class FilterToSqlWhere
    {
        internal static string ToSqlWhere(FilterOperation filter) {
            var result = Traverse(filter);
            return "WHERE " + result;
        }
        
        private static string Traverse(Operation filter) {
            switch (filter) {
                case Field field:
                    return $"c{field.name}";
                case StringLiteral stringLiteral:
                    return $"'{stringLiteral.value}'";
                case Equal equal:
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    return $"{left} = {right}";
                case Any any:
                    return "xxx";
                default:
                    throw new NotImplementedException($"missing conversion of filter: {filter}");
            }
        }
    }
}