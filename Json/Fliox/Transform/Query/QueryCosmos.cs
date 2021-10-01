// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query
{
    internal static class QueryCosmos
    {
        internal static string ToCosmos(string collection, FilterOperation filter) {
            var cx      = new ConvertContext(collection, filter);
            var result  = cx.Traverse(filter);
            return "WHERE " + result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   string           collection;
        private readonly   FilterOperation  filter;
        
        internal ConvertContext (string collection, FilterOperation filter) {
            this.collection = collection;
            this.filter     = filter;
        }
        
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field:
                    return $"{collection}{field.name}";
                
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
                
                case Not @not:
                    var operand = Traverse(@not.operand);
                    return $"!({operand})";
                
                case Or or:
                    var operands = GetOperands(or.operands);
                    return string.Join(" || ", operands);
                case And and:
                    operands = GetOperands(and.operands);
                    return string.Join(" && ", operands);
                    
                case Any any:
                    var cx              = new ConvertContext ("", filter);
                    operand             = cx.Traverse(any.predicate);
                    string fieldName    = Traverse(any.field);
                    string arg          = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                case All all:
                    cx                  = new ConvertContext ("", filter);
                    operand             = cx.Traverse(all.predicate);
                    fieldName           = Traverse(all.field);
                    arg                 = all.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                case CountWhere countWhere:
                    cx                  = new ConvertContext ("", filter);
                    operand             = cx.Traverse(countWhere.predicate);
                    fieldName           = Traverse(countWhere.field);
                    arg                 = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {filter}");
            }
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                result[n] = Traverse(operands[n]);
            }
            return result;
        }
    }
}