// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLProvider;
using static Friflo.Json.Fliox.Transform.OpType;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal enum MySQLProvider {
        MY_SQL,
        MARIA_DB
    }
    
    public static class MySQLExtensions
    {
        public static string MySQLFilter  (this FilterOperation op, TableType tableType) => MySQLFilter(op, MY_SQL,   tableType);
        public static string MariaDBFilter(this FilterOperation op, TableType tableType) => MySQLFilter(op, MARIA_DB, tableType);

        internal static string MySQLFilter(this FilterOperation op, MySQLProvider provider, TableType tableType) {
            var filter      = (Filter)op;
            var args        = new FilterArgs(filter);
            args.AddArg(filter.arg, DATA);
            var cx          = new ConvertContext (args, provider, tableType);
            var result      = cx.Traverse(filter.body);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   FilterArgs       args;
        private readonly   MySQLProvider    provider;
        private readonly   TableType        tableType;
        
        internal ConvertContext (FilterArgs args, MySQLProvider provider, TableType tableType) {
            this.args       = args;
            this.provider   = provider;
            this.tableType  = tableType;
        }
        
        internal static string GetSqlType(ColumnInfo column, MySQLProvider provider) {
            switch (column.type) {
                case ColumnType.Uint8:      return "tinyint unsigned";
                case ColumnType.Int16:      return "smallint";
                case ColumnType.Int32:      return "integer";
                case ColumnType.Int64:      return "bigint";
                case ColumnType.Float:      return "float";
                case ColumnType.Double:     return "double precision";
                case ColumnType.Boolean:    return "tinyint";
                case ColumnType.Guid:       return provider == MY_SQL ? "varchar(36)" : "UUID";
                case ColumnType.DateTime:   return "DATETIME(6)";
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:       return column.isPrimaryKey ? "varchar(255)" : "text"; // used 255 to enable using as PK
                case ColumnType.JsonValue:
                case ColumnType.Array:      return "JSON"; // JSON column
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
                    // TODO - simplify
                    var field       = (Field)operation;
                    var path        = GetFieldPath(field);
                    var arrayField  = args.GetArrayField(field);
                    if (tableType == TableType.Relational) {
                        if (arrayField != null) {
                            return $"JSON_VALUE({arrayField.array}, '{path}')";
                        }
                        return GetColumn(field);
                    }
                    if (arrayField != null) {
                        return $"JSON_VALUE({arrayField.array}, '{path}')";
                    }
                    var arg  = args.GetArg(field);
                    return $"JSON_VALUE({arg},'{path}')";
                }
                
                // --- literal --- 
                case STRING: {
                    var str     = (StringLiteral)operation;
                    var value   = EscapeMySql(str.value); 
                    return SQLUtils.ToSqlString(value, "CONCAT(", ",", ")", "CHAR");
                }
                case DOUBLE:
                    var doubleLiteral = (DoubleLiteral)operation;
                    return doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                case INT64:
                    var longLiteral = (LongLiteral)operation;
                    return longLiteral.value.ToString();
                case TRUE:
                    return True();
                case FALSE:
                    return False();
                case NULL:
                    return "null";
                
                // --- compare ---
                case EQUAL: {
                    var equal = (Equal)operation;
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    // e.g. WHERE json_extract({DATA},'$.int32') is null || JSON_TYPE(json_extract({DATA},'$.int32')) = 'NULL'
                    if (left  == "null") return $"({right} is null)";
                    if (right == "null") return $"({left} is null)";
                    return $"{left} = {right}";
                }
                case NOT_EQUAL: {
                    var notEqual = (NotEqual)operation;
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    // e.g WHERE json_extract({DATA},'$.int32') is not null && JSON_TYPE(json_extract({DATA},'$.int32')) != 'NULL'
                    if (left  == "null") return $"({right} is not null)";
                    if (right == "null") return $"({left} is not null)";
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
                    return $"{left} LIKE CONCAT({right},'%')";
                }
                case ENDS_WITH: {
                    var endsWith = (EndsWith)operation;
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"{left} LIKE CONCAT('%',{right})";
                }
                case CONTAINS: {
                    var contains = (Contains)operation;
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"{left} LIKE CONCAT('%',{right},'%')";
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
            var select          = provider == MY_SQL ? "1" : column;
            return
$@"FALSE OR EXISTS(
    SELECT {select}
    FROM JSON_TABLE({column}, '{arrayPath}[*]' COLUMNS({arrayTable} JSON PATH '$')) as jt
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
            var select          = provider == MY_SQL ? "1" : column;
            return
$@"NOT EXISTS(
    SELECT {select}
    FROM JSON_TABLE({column}, '{arrayPath}[*]' COLUMNS({arrayTable} JSON PATH '$')) as jt
    WHERE NOT ({operand})
)";
        }
        
        private string True()  => tableType == TableType.Relational ? "true"  : provider == MY_SQL ? "'true'"  : "true";
        private string False() => tableType == TableType.Relational ? "false" : provider == MY_SQL ? "'false'" : "false";
        
        private string ToBoolean(string operand) {
            if (provider == MY_SQL) {
                switch (operand) {
                    case "'true'":  return "true";
                    case "'false'": return "false";
                }
            }
            return operand;
        }
        
        // [MySQL :: MySQL 8.0 Reference Manual :: 9.1.1 String Literals] https://dev.mysql.com/doc/refman/8.0/en/string-literals.html
        private static string EscapeMySql(string value) {
            var sb = new StringBuilder();
            foreach (var c in value) {
                switch (c) {
                    case '\'':  sb.Append("''");    continue;
                    case '"':   sb.Append("\\\"");  continue;
                    case '\\':  sb.Append("\\\\");  continue;
                    case '%':   sb.Append("\\%");   continue;
                    case '_':   sb.Append("\\_");   continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                var operand = Traverse(operands[n]);
                operand     = ToBoolean(operand);
                result[n]   = operand;
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
            return $"`{name.Substring(field.arg.Length + 1)}`";
        }
    }
}