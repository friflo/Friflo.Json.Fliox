// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public static class CosmosExtensions
    {
        public static string CosmosFilter(this FilterOperation op) {
            var cx      = new ConvertContext("c", op);
            var result  = cx.Traverse(op);
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
                    /* if (collectionStart != null && field.name.StartsWith(collectionStart)) {
                        var fieldName   = field.name.Substring(collectionStart.Length);
                        var path        = ConvertPath(fieldName);
                        return $"{collection}{path}";
                    } */
                    var firstDot = field.name.IndexOf('.');
                    if (firstDot != - 1) {
                        var fieldName       = field.name.Substring(firstDot + 1);
                        var path            = ConvertPath(fieldName);
                        var collectionName  = collection ?? field.name.Substring(0, firstDot);
                        return $"{collectionName}{path}";
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
                    if (left  == "null") return $"(IS_NULL({right}) OR NOT IS_DEFINED({right}))";
                    if (right == "null") return $"(IS_NULL({left}) OR NOT IS_DEFINED({left}))";
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
                
                // --- arithmetic: operators ---
                case Add add:
                    left    = Traverse(add.left);
                    right   = Traverse(add.right);
                    return $"{left} + {right}";
                case Subtract subtract:
                    left    = Traverse(subtract.left);
                    right   = Traverse(subtract.right);
                    return $"{left} - {right}";
                case Multiply multiply:
                    left    = Traverse(multiply.left);
                    right   = Traverse(multiply.right);
                    return $"{left} * {right}";
                case Divide divide:
                    left    = Traverse(divide.left);
                    right   = Traverse(divide.right);
                    return $"{left} / {right}";
                case Modulo modulo:
                    left    = Traverse(modulo.left);
                    right   = Traverse(modulo.right);
                    return $"{left} % {right}";
                
                // --- arithmetic: methods ---
                case Abs abs:
                    value = Traverse(abs.value);
                    return $"ABS({value})";
                case Ceiling ceiling:
                    value = Traverse(ceiling.value);
                    return $"CEILING({value})";
                case Floor floor:
                    value = Traverse(floor.value);
                    return $"FLOOR({value})";
                case Exp exp:
                    value = Traverse(exp.value);
                    return $"EXP({value})";
                case Log log:
                    value = Traverse(log.value);
                    return $"LOG({value})";
                case Sqrt sqrt:
                    value = Traverse(sqrt.value);
                    return $"SQRT({value})";
                
                // --- constants ---
                case PiLiteral:
                    return "PI()";
                case EulerLiteral:
                    return "EXP(1)";
                
                // --- aggregate ---
                case CountWhere countWhere: {
                    var cx              = new ConvertContext (null, filterOp);
                    operand             = cx.Traverse(countWhere.predicate);
                    string fieldName    = Traverse(countWhere.field);
                    string arg          = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                }
                // --- quantify ---
                case Any any: {
                    var cx              = new ConvertContext (null, filterOp);
                    operand             = cx.Traverse(any.predicate);
                    var fieldName       = Traverse(any.field);
                    var arg             = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                }
                case All all: {
                    var cx              = new ConvertContext (null, filterOp);
                    operand             = cx.Traverse(all.predicate);
                    var fieldName       = Traverse(all.field);
                    var arg            = all.arg;
                    // treat array == null and missing array as empty array <=> array[]
                    return $"IS_NULL({fieldName}) OR NOT IS_DEFINED({fieldName}) OR (SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                }
                // --- query filter expression
                case Filter filter: {
                    var cx              = new ConvertContext (collection, filterOp);
                    operand             = cx.Traverse(filter.body);
                    return $"{operand}";
                }
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
        
        // SQL statements in CosmosDB must not used reserved SQL keywords to access fields in the WHERE clause. E.g.
        // SELECT * FROM c WHERE c.value = 0
        // errors because value (VALUE, Value, ...) is a reserved SQL keyword 
        private static string ConvertPath (string path) {
            var names = path.Split();
            var cosmosNames = names.Select(name => $"['{name}']");
            return string.Join(null, cosmosNames);
        }
    }
}