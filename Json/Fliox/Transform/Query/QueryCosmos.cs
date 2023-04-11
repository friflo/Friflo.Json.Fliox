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
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    if (collectionStart != null && field.name.StartsWith(collectionStart)) {
                        return $"{collection}.{field.name.Substring(collectionStart.Length)}";
                    }
                    return field.name;
                }
                
                // --- literal --- 
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
                
                // --- compare ---
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
                
                // --- logical ---
                case Not @not:
                    var operand = Traverse(@not.operand);
                    return $"NOT({operand})";
                case Or or:
                    var operands = GetOperands(or.operands);
                    return string.Join(" OR ", operands);
                case And and:
                    operands = GetOperands(and.operands);
                    return string.Join(" AND ", operands);
                
                // --- string ---
                case StartsWith startsWith:
                    left    = Traverse(startsWith.left);
                    right   = Traverse(startsWith.right);
                    return $"STARTSWITH({left},{right})";
                case EndsWith endsWith:
                    left    = Traverse(endsWith.left);
                    right   = Traverse(endsWith.right);
                    return $"ENDSWITH({left},{right})";
                case Contains contains:
                    left    = Traverse(contains.left);
                    right   = Traverse(contains.right);
                    return $"CONTAINS({left},{right})";
                case Length length:
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                
                // --- arithmetic ---
                
                // --- constants ---
                
                // --- aggregate ---
                case CountWhere countWhere:
                    var cx              = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(countWhere.predicate);
                    string fieldName    = Traverse(countWhere.field);
                    string arg          = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";

                // --- quantify ---
                case Any any:
                    cx                  = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(any.predicate);
                    fieldName           = Traverse(any.field);
                    arg                 = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                case All all:
                    cx                  = new ConvertContext ("", filterOp);
                    operand             = cx.Traverse(all.predicate);
                    fieldName           = Traverse(all.field);
                    arg                 = all.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                
                // --- query filter expression
                case Filter filter:
                    cx                  = new ConvertContext (collection, filterOp);
                    operand             = cx.Traverse(filter.body);
                    return $"{operand}";
                
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