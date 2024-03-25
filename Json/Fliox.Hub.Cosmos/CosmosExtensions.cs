// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using static Friflo.Json.Fliox.Transform.OpType;

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
            switch (operation.Type) {
                case FIELD: {
                    var field       = (Field)operation;
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
                case STRING: {
                    var str     = (StringLiteral)operation;
                    return EscapeCosmosDB(str.value);
                }
                case DOUBLE:
                    var doubleLiteral = (DoubleLiteral)operation;
                    return doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                case INT64:
                    var longLiteral = (LongLiteral)operation;
                    return longLiteral.value.ToString();
                case TRUE:
                    return "true";
                case FALSE:
                    return "false";
                case NULL:
                    return "null";
                
                // --- compare ---
                case EQUAL: {
                    var equal = (Equal)operation;
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    if (left  == "null") return $"(IS_NULL({right}) OR NOT IS_DEFINED({right}))";
                    if (right == "null") return $"(IS_NULL({left}) OR NOT IS_DEFINED({left}))";
                    return $"{left} = {right}";
                }
                case NOT_EQUAL: {
                    var notEqual = (NotEqual)operation;
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"(NOT IS_NULL({right}) AND IS_DEFINED({right}))";
                    if (right == "null") return $"(NOT IS_NULL({left}) AND IS_DEFINED({left}))";
                    return $"(NOT IS_DEFINED({left}) or NOT IS_DEFINED({right}) or {left} != {right})";
                }
                case LESS: {
                    var lessThan = (Less)operation;
                    var left    = Traverse(lessThan.left);
                    var right   = Traverse(lessThan.right);
                    return $"{left} < {right}";
                }
                case LESS_OR_EQUAL: {
                    var lessThanOrEqual = (LessOrEqual)operation;
                    var left    = Traverse(lessThanOrEqual.left);
                    var right   = Traverse(lessThanOrEqual.right);
                    return $"{left} <= {right}";
                }
                case GREATER: {
                    var greaterThan = (Greater)operation;
                    var left    = Traverse(greaterThan.left);
                    var right   = Traverse(greaterThan.right);
                    return $"{left} > {right}";
                }
                case GREATER_OR_EQUAL: {
                    var greaterThanOrEqual = (GreaterOrEqual)operation;
                    var left    = Traverse(greaterThanOrEqual.left);
                    var right   = Traverse(greaterThanOrEqual.right);
                    return $"{left} >= {right}";
                }
                
                // --- logical ---
                case NOT: {
                    var @not = (Not)operation;
                    var operand = Traverse(@not.operand);
                    return $"NOT({operand})";
                }
                case OR: {
                    var or = (Or)operation;
                    var operands = GetOperands(or.operands);
                    return string.Join(" OR ", operands);
                }
                case AND: {
                    var and = (And)operation;
                    var operands = GetOperands(and.operands);
                    return string.Join(" AND ", operands);
                }
                
                // --- string ---
                case STARTS_WITH: {
                    var startsWith = (StartsWith)operation;
                    var left    = Traverse(startsWith.left);
                    var right   = Traverse(startsWith.right);
                    return $"STARTSWITH({left},{right})";
                }
                case ENDS_WITH: {
                    var endsWith = (EndsWith)operation;
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"ENDSWITH({left},{right})";
                }
                case CONTAINS: {
                    var contains = (Contains)operation;
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"CONTAINS({left},{right})";
                }
                case LENGTH: {
                    var length = (Length)operation;
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                }
                
                // --- arithmetic: operators ---
                case ADD: {
                    var add = (Add)operation;
                    var left    = Traverse(add.left);
                    var right   = Traverse(add.right);
                    return $"{left} + {right}";
                }
                case SUBTRACT: {
                    var subtract = (Subtract)operation;
                    var left    = Traverse(subtract.left);
                    var right   = Traverse(subtract.right);
                    return $"{left} - {right}";
                }
                case MULTIPLY: {
                    var multiply = (Multiply)operation;
                    var left    = Traverse(multiply.left);
                    var right   = Traverse(multiply.right);
                    return $"{left} * {right}";
                }
                case DIVIDE: {
                    var divide = (Divide)operation;
                    var left    = Traverse(divide.left);
                    var right   = Traverse(divide.right);
                    return $"{left} / {right}";
                }
                case MODULO: {
                    var modulo = (Modulo)operation;
                    var left    = Traverse(modulo.left);
                    var right   = Traverse(modulo.right);
                    return $"{left} % {right}";
                }
                
                // --- arithmetic: methods ---
                case ABS: {
                    var abs = (Abs)operation;
                    var value = Traverse(abs.value);
                    return $"ABS({value})";
                }
                case CEILING: {
                    var ceiling = (Ceiling)operation;
                    var value = Traverse(ceiling.value);
                    return $"CEILING({value})";
                }
                case FLOOR: {
                    var floor = (Floor)operation;
                    var value = Traverse(floor.value);
                    return $"FLOOR({value})";
                }
                case EXP: {
                    var exp = (Exp)operation;
                    var value = Traverse(exp.value);
                    return $"EXP({value})";
                }
                case LOG: {
                    var log = (Log)operation;
                    var value = Traverse(log.value);
                    return $"LOG({value})";
                }
                case SQRT: {
                    var sqrt = (Sqrt)operation;
                    var value = Traverse(sqrt.value);
                    return $"SQRT({value})";
                }
                case NEGATE: {
                    var negate = (Negate)operation;
                    var value = Traverse(negate.value);
                    return $"-({value})";
                }
                
                // --- constants ---
                case PI:
                    return "PI()";
                case E:
                    return "EXP(1)";
                
                // --- aggregate ---
                case MIN:
                case MAX:
                case SUM:
                case AVERAGE:
                case COUNT:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {args.filter}");
                case COUNT_WHERE:
                    return TraverseCount((CountWhere)operation);
                // --- quantify ---
                case ANY:
                    return TraverseAny((Any)operation);
                case ALL:
                    return TraverseAll((All)operation);
                
                case TAU:
                case LAMBDA:
                case FILTER:
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
        
        // [SQL constants in Azure Cosmos DB | Microsoft Learn] https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/constants#bk_arguments
        private static string EscapeCosmosDB(string value) {
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var c in value) {
                switch (c) {
                    case '\'':  sb.Append("\\'");   continue;
                    case '"':   sb.Append("\\\"");  continue;
                    case '\\':  sb.Append("\\\\");  continue;
                    case '/':   sb.Append("\\/");   continue;
                    /*
                    case '\b':  sb.Append("\\b");   continue;                    
                    case '\f':  sb.Append("\\f");   continue;
                    case '\n':  sb.Append("\\n");   continue;
                    case '\r':  sb.Append("\\r");   continue;
                    case '\t':  sb.Append("\\t");   continue;
                    */
                }
                /*if (c < 32) {
                    var i = (int)c;
                    sb.Append("\\u");
                    sb.Append(i.ToString("x4"));
                    continue;
                }*/
                sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}