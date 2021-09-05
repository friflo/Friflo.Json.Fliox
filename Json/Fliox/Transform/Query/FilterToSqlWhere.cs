// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query
{
    internal static class FilterToSqlWhere
    {
        internal static string ToSqlWhere(FilterOperation filter) {
            var cx      = new ConvertContext(filter);
            var result  = cx.Traverse(filter);
            return "WHERE " + result;
        }
    }
    
    internal class ConvertContext {
        private readonly   FilterOperation     filter;
        
        internal ConvertContext (FilterOperation filter) {
            this.filter = filter;
        }
        
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field:
                    return $"c{field.name}";
                
                case StringLiteral stringLiteral:
                    return $"'{stringLiteral.value}'";
                case DoubleLiteral doubleLiteral:
                    return doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                case LongLiteral longLiteral:
                    return longLiteral.value.ToString();
                case TrueLiteral trueLiteral:
                    return "true";
                case FalseLiteral falseLiteral:
                    return "false";
                case NullLiteral nullLiteral:
                    return "null";
                
                case Equal equal:
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    return $"{left} = {right}";
                case NotEqual notEqual:
                    left    = Traverse(notEqual.left);
                    right   = Traverse(notEqual.right);
                    return $"{left} != {right}";
                case LessThan lessThan:
                    left    = Traverse(lessThan.left);
                    right   = Traverse(lessThan.right);
                    return $"{left} < {right}";
                case LessThanOrEqual lessThanOrEqual:
                    left    = Traverse(lessThanOrEqual.left);
                    right   = Traverse(lessThanOrEqual.right);
                    return $"{left} <= {right}";
                case GreaterThan greaterThan:
                    left    = Traverse(greaterThan.left);
                    right   = Traverse(greaterThan.right);
                    return $"{left} > {right}";
                case GreaterThanOrEqual greaterThanOrEqual:
                    left    = Traverse(greaterThanOrEqual.left);
                    right   = Traverse(greaterThanOrEqual.right);
                    return $"{left} >= {right}";

                case Any any:
                    return "xxx";
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {filter}");
            }
        }
    }
}