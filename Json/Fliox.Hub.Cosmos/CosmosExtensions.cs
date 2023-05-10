// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public static class CosmosExtensions
    {
        public static string CosmosFilter(this FilterOperation op) {
            var args        = new FilterArgs(op);
            var cx          = new ConvertContext (args);
            if (op is Filter filter) {
                args.AddArg(filter.arg, "c");
                return cx.Traverse(filter.body);
            }
            return cx.Traverse(op);
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   FilterArgs       args;
        
        internal ConvertContext (FilterArgs       args) {
            this.args = args;
        }
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    var arg  = args.GetArg(field);
                    var firstDot = field.name.IndexOf('.');
                    if (firstDot != - 1) {
                        var fieldName       = field.name.Substring(firstDot + 1);
                        var path            = ConvertPath(fieldName);
                        return $"{arg}{path}";
                    }
                    return arg;
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
                case Equal equal: {
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    if (left  == "null") return $"(IS_NULL({right}) OR NOT IS_DEFINED({right}))";
                    if (right == "null") return $"(IS_NULL({left}) OR NOT IS_DEFINED({left}))";
                    return $"{left} = {right}";
                }
                case NotEqual notEqual: {
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"(NOT IS_NULL({right}) AND IS_DEFINED({right}))";
                    if (right == "null") return $"(NOT IS_NULL({left}) AND IS_DEFINED({left}))";
                    return $"(NOT IS_DEFINED({left}) or NOT IS_DEFINED({right}) or {left} != {right})";
                }
                case Less lessThan: {
                    var left    = Traverse(lessThan.left);
                    var right   = Traverse(lessThan.right);
                    return $"{left} < {right}";
                }
                case LessOrEqual lessThanOrEqual: {
                    var left    = Traverse(lessThanOrEqual.left);
                    var right   = Traverse(lessThanOrEqual.right);
                    return $"{left} <= {right}";
                }
                case Greater greaterThan: {
                    var left    = Traverse(greaterThan.left);
                    var right   = Traverse(greaterThan.right);
                    return $"{left} > {right}";
                }
                case GreaterOrEqual greaterThanOrEqual: {
                    var left    = Traverse(greaterThanOrEqual.left);
                    var right   = Traverse(greaterThanOrEqual.right);
                    return $"{left} >= {right}";
                }
                
                // --- logical ---
                case Not @not: {
                    var operand = Traverse(@not.operand);
                    return $"NOT({operand})";
                }
                case Or or: {
                    var operands = GetOperands(or.operands);
                    return string.Join(" OR ", operands);
                }
                case And and: {
                    var operands = GetOperands(and.operands);
                    return string.Join(" AND ", operands);
                }
                
                // --- string ---
                case StartsWith startsWith: {
                    var left    = Traverse(startsWith.left);
                    var right   = Traverse(startsWith.right);
                    return $"STARTSWITH({left},{right})";
                }
                case EndsWith endsWith: {
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"ENDSWITH({left},{right})";
                }
                case Contains contains: {
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"CONTAINS({left},{right})";
                }
                case Length length: {
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                }
                
                // --- arithmetic: operators ---
                case Add add: {
                    var left    = Traverse(add.left);
                    var right   = Traverse(add.right);
                    return $"{left} + {right}";
                }
                case Subtract subtract: {
                    var left    = Traverse(subtract.left);
                    var right   = Traverse(subtract.right);
                    return $"{left} - {right}";
                }
                case Multiply multiply: {
                    var left    = Traverse(multiply.left);
                    var right   = Traverse(multiply.right);
                    return $"{left} * {right}";
                }
                case Divide divide: {
                    var left    = Traverse(divide.left);
                    var right   = Traverse(divide.right);
                    return $"{left} / {right}";
                }
                case Modulo modulo: {
                    var left    = Traverse(modulo.left);
                    var right   = Traverse(modulo.right);
                    return $"{left} % {right}";
                }
                
                // --- arithmetic: methods ---
                case Abs abs: {
                    var value = Traverse(abs.value);
                    return $"ABS({value})";
                }
                case Ceiling ceiling: {
                    var value = Traverse(ceiling.value);
                    return $"CEILING({value})";
                }
                case Floor floor: {
                    var value = Traverse(floor.value);
                    return $"FLOOR({value})";
                }
                case Exp exp: {
                    var value = Traverse(exp.value);
                    return $"EXP({value})";
                }
                case Log log: {
                    var value = Traverse(log.value);
                    return $"LOG({value})";
                }
                case Sqrt sqrt: {
                    var value = Traverse(sqrt.value);
                    return $"SQRT({value})";
                }
                
                // --- constants ---
                case PiLiteral:
                    return "PI()";
                case EulerLiteral:
                    return "EXP(1)";
                
                // --- aggregate ---
                case CountWhere countWhere:
                    return TraverseCount(countWhere);
                // --- quantify ---
                case Any any:
                    return TraverseAny(any);
                case All all:
                    return TraverseAll(all);
                
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {args.filter}");
            }
        }
        
        private string TraverseCount (CountWhere countWhere) {
            string arg          = countWhere.arg;
            using var scope     = args.AddArg(arg);
            var operand         = Traverse(countWhere.predicate);
            string fieldName    = Traverse(countWhere.field);
            return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
        }
        
        private string TraverseAny (Any any) {
            var arg             = any.arg;
            using var scope     = args.AddArg(arg);
            var operand         = Traverse(any.predicate);
            var fieldName       = Traverse(any.field);
            return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
        }
        
        private string TraverseAll (All all) {
            var arg             = all.arg;
            using var scope     = args.AddArg(arg);
            var operand         = Traverse(all.predicate);
            var fieldName       = Traverse(all.field);
            // treat array == null and missing array as empty array <=> array[]
            return $"IS_NULL({fieldName}) OR NOT IS_DEFINED({fieldName}) OR (SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
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
            var names = path.Split('.');
            var cosmosNames = names.Select(name => $"['{name}']");
            return string.Join(null, cosmosNames);
        }
    }
}