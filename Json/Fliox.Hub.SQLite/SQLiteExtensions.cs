// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable UseNegatedPatternMatching
// ReSharper disable ReplaceSubstringWithRangeIndexer
namespace Friflo.Json.Fliox.Hub.SQLite
{
    public static class SQLiteExtensions
    {
        public static string SQLiteFilter(this FilterOperation op) {
            var filter      = (Filter)op;
            var args        = new FilterArgs(filter);
            args.AddArg(filter.arg, "data");
            var cx          = new ConvertContext (args);
            var result      = cx.Traverse(filter.body);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly    FilterArgs  args;

        internal ConvertContext (FilterArgs args) {
            this.args       = args;
        }

        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    var arg         = args.GetArg(field);
                    var path        = GetFieldPath(field);
                    var arrayField  = args.GetArrayField(field);
                    if (arrayField != null) {
                        return $"json_extract({arrayField.array}.value, '{path}')";
                    }
                    return $"json_extract({arg},'{path}')";
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
                    if (left  == "null") return $"({right} IS null)";
                    if (right == "null") return $"({left} IS null)";
                    return $"{left} = {right}";
                }
                case NotEqual notEqual: {
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"({right} IS NOT null)";
                    if (right == "null") return $"({left} IS NOT null)";
                    return $"({left} is null or {right} is null or {left} != {right})";
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
                    return $"{left} LIKE '{UnString(right)}%'";
                }
                case EndsWith endsWith: {
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"{left} LIKE '%{UnString(right)}'";
                }
                case Contains contains: {
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"{left} LIKE '%{UnString(right)}%'";
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
                    return $"ROUND({value}+0.5)";
                }
                case Floor floor: {
                    var value = Traverse(floor.value);
                    return $"ROUND({value}-0.5)";
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
                    return "3.141592653589793"; // return "PI()";
                case EulerLiteral:
                    return "2.718281828459045"; // return "EXP(1)";
                
                // --- aggregate ---
                case CountWhere countWhere: {
                    string arg          = countWhere.arg;
                    var arrayTable      = "je_array";
                    using var scope     = args.AddArg(arg);
                    using var array     = args.AddArrayField(arg, arrayTable);
                    var operand         = Traverse(countWhere.predicate);
                    string fieldName    = Traverse(countWhere.field);
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                }
                // --- quantify ---
                case Any any: {
                    var arg             = any.arg;
                    var arrayTable      = "je_array";
                    using var scope     = args.AddArg(arg);
                    using var array     = args.AddArrayField(arg, arrayTable);
                    var operand         = Traverse(any.predicate);
                    string arrayPath    = GetFieldPath(any.field);
                    return
$@"EXISTS(
    SELECT 1
    FROM json_each(data, '{arrayPath}') as {arrayTable}
    WHERE {operand}
)";
                }
                case All all: {
                    var arg             = all.arg;
                    var arrayTable      = "je_array";
                    using var scope     = args.AddArg(arg);
                    using var array     = args.AddArrayField(arg, arrayTable);
                    var operand         = Traverse(all.predicate);
                    string arrayPath    = GetFieldPath(all.field);
                    return
$@"NOT EXISTS(
    SELECT 1
    FROM json_each(data, '{arrayPath}') as {arrayTable}
    WHERE NOT ({operand})
)";
                }
                /* // --- query filter expression
                case Filter filter: {
                    var arg             = filter.arg;
                    using var cx        = new ConvertContext (args, arg);
                    operand             = cx.Traverse(filter.body);
                    return $"{operand}";
                } */
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {args.filter}");
            }
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                result[n] = Traverse(operands[n]);
            }
            return result;
        }
        
        private static string UnString(string value) {
            if (value[0] == '\'') {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
        
        private static string GetFieldPath(Field field) {
            if (field.arg == field.name) {
                return "$";
            }
            var path = field.name.Substring(field.arg.Length + 1);
            return $"$.{path}";
        }
    }
}