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
        internal static string ToCosmos(string collection, FilterOperation filterOperation) {
            var cx      = new ConvertContext(collection, filterOperation);
            var result  = cx.Traverse(filterOperation);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   string           collection;
        private readonly   string           collectionStart;
        private readonly   FilterOperation  filterOp;
        
        internal ConvertContext (string collection, FilterOperation filterOp) {
            this.collection = collection;
            if (filterOp is Filter filter) {
                collectionStart = $"{filter.arg}.";
            }
            this.filterOp     = filterOp;
        }
        
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    if (collectionStart != null && field.name.StartsWith(collectionStart)) {
                        return $"{collection}.{field.name.Substring(collectionStart.Length)}";
                    }
                    return field.name;
                }
                case StringLiteral stringLiteral:
                    return $"'{stringLiteral.value}'";
                case DoubleLiteral doubleLiteral:
                    return doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                case LongLiteral longLiteral:
                    return longLiteral.value.ToString();
                case TrueLiteral    _:
                    return "true";
                case FalseLiteral   _:
                    return "false";
                case NullLiteral    _:
                    return "null";
                
                case Equal equal:
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    return $"{left} = {right}";
                case NotEqual notEqual:
                    left    = Traverse(notEqual.left);
                    right   = Traverse(notEqual.right);
                    return $"{left} != {right}";
                case Less lessThan:
                    left    = Traverse(lessThan.left);
                    right   = Traverse(lessThan.right);
                    return $"{left} < {right}";
                case LessOrEqual lessThanOrEqual:
                    left    = Traverse(lessThanOrEqual.left);
                    right   = Traverse(lessThanOrEqual.right);
                    return $"{left} <= {right}";
                case Greater greaterThan:
                    left    = Traverse(greaterThan.left);
                    right   = Traverse(greaterThan.right);
                    return $"{left} > {right}";
                case GreaterOrEqual greaterThanOrEqual:
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
                
                case Length length:
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                
                case Filter filterOp:
                    var cx              = new ConvertContext (collection, this.filterOp);
                    operand             = cx.Traverse(filterOp.body);
                    return $"{operand}";
                case Any any:
                    cx                  = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(any.predicate);
                    string fieldName    = Traverse(any.field);
                    string arg          = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                case All all:
                    cx                  = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(all.predicate);
                    fieldName           = Traverse(all.field);
                    arg                 = all.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                case CountWhere countWhere:
                    cx                  = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(countWhere.predicate);
                    fieldName           = Traverse(countWhere.field);
                    arg                 = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {filterOp}");
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