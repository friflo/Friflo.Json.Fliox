// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using static Friflo.Json.Fliox.Transform.OpType;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseNegatedPatternMatching
// ReSharper disable ReplaceSubstringWithRangeIndexer
namespace Friflo.Json.Fliox.Hub.SQLite
{
    public static class SQLiteExtensions
    {
        public static string SQLiteFilter(this FilterOperation op, TableType tableType) {
            var filter      = (Filter)op;
            var args        = new FilterArgs(filter);
            args.AddArg(filter.arg, DATA);
            var cx          = new ConvertContext (args, tableType);
            var result      = cx.Traverse(filter.body);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly    FilterArgs  args;
        private readonly    TableType   tableType;

        internal ConvertContext (FilterArgs args, TableType tableType) {
            this.args       = args;
            this.tableType  = tableType;
        }
        
        internal static string GetSqlType(ColumnInfo column) {
            switch (column.type) {
                case ColumnType.Uint8:
                case ColumnType.Int16:
                case ColumnType.Int32:
                case ColumnType.Int64:      return "integer";
                case ColumnType.Float:
                case ColumnType.Double:     return "real";
                case ColumnType.Boolean:    return "tinyint";
                case ColumnType.DateTime:
                case ColumnType.Guid:
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:       return "text";
                case ColumnType.JsonValue:
                case ColumnType.Array:      return "text"; // JSON column
                case ColumnType.Object:     return "tinyint";
            }
            throw new NotSupportedException($"column type: {column.type}");
        }

        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation.Type) {
                case FIELD: {
                    var field       = (Field)operation;
                    var arg         = args.GetArg(field);
                    var path        = GetFieldPath(field);
                    var arrayField  = args.GetArrayField(field);
                    if (tableType == TableType.Relational) {
                        if (arrayField != null) {
                            return $"json_extract({arrayField.array}.value, '{path}')";
                        }
                        return GetColumn(field);
                    }
                    if (arrayField != null) {
                        return $"json_extract({arrayField.array}.value, '{path}')";
                    }
                    return $"json_extract({arg},'{path}')";
                }
                
                // --- literal --- 
                case STRING: {
                    var str     = (StringLiteral)operation;
                    var value   = SQLUtils.Escape(str.value);
                    return SQLUtils.ToSqlString(value, "(", "||", ")", "CHAR");
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
                    if (left  == "null") return $"({right} IS null)";
                    if (right == "null") return $"({left} IS null)";
                    return $"{left} = {right}";
                }
                case NOT_EQUAL: {
                    var notEqual = (NotEqual)operation;
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"({right} IS NOT null)";
                    if (right == "null") return $"({left} IS NOT null)";
                    return $"({left} is null or {right} is null or {left} != {right})";
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
                    return $"{left} LIKE {right}||'%'";
                }
                case ENDS_WITH: {
                    var endsWith = (EndsWith)operation;
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"{left} LIKE '%'||{right}";
                }
                case CONTAINS: {
                    var contains = (Contains)operation;
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"{left} LIKE '%'||{right}||'%'";
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
                    return $"ROUND({value}+0.5)";
                }
                case FLOOR: {
                    var floor = (Floor)operation;
                    var value = Traverse(floor.value);
                    return $"ROUND({value}-0.5)";
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
                    return "3.141592653589793"; // return "PI()";
                case E:
                    return "2.718281828459045"; // return "EXP(1)";
                
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
            var arrayTable      = "je_array";
            using var scope     = args.AddArg(arg);
            using var array     = args.AddArrayField(arg, arrayTable);
            var operand         = Traverse(countWhere.predicate);
            string fieldName    = Traverse(countWhere.field);
            return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
        }
        
        private string TraverseAny (Any any) {
            var arg             = any.arg;
            var arrayTable      = "je_array";
            using var scope     = args.AddArg(arg);
            using var array     = args.AddArrayField(arg, arrayTable);
            var operand         = Traverse(any.predicate);
            var column          = tableType == TableType.JsonColumn ? DATA : GetColumn(any.field);
            var arrayPath       = tableType == TableType.JsonColumn ? GetFieldPath(any.field) : "$";
            return
$@"EXISTS(
    SELECT 1
    FROM json_each({column}, '{arrayPath}') as {arrayTable}
    WHERE {operand}
)";
        }
        
        private string TraverseAll (All all) {
            var arg             = all.arg;
            var arrayTable      = "je_array";
            using var scope     = args.AddArg(arg);
            using var array     = args.AddArrayField(arg, arrayTable);
            var operand         = Traverse(all.predicate);
            var column          = tableType == TableType.JsonColumn ? DATA : GetColumn(all.field);
            var arrayPath       = tableType == TableType.JsonColumn ? GetFieldPath(all.field) : "$";
            return
$@"NOT EXISTS(
    SELECT 1
    FROM json_each({column}, '{arrayPath}') as {arrayTable}
    WHERE NOT ({operand})
)";
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                result[n] = Traverse(operands[n]);
            }
            return result;
        }
        
        private static string GetFieldPath(Field field) {
            if (field.arg == field.name) {
                return "$";
            }
            var path = field.name.Substring(field.arg.Length + 1);
            return $"$.{path}";
        }
        
        private static string GetColumn(Field field) {
            var name = field.name;
            if (field.arg == name) {
                //return "$";
                throw new NotSupportedException("GetColum()");
            }
            return $"[{name.Substring(field.arg.Length + 1)}]";
        }
    }
}