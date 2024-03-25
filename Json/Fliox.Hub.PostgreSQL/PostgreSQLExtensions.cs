// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using static Friflo.Json.Fliox.Transform.OpType;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLExtensions
    {
        public static string PostgresFilter(this FilterOperation op, TypeDef entityType, TableType tableType) {
            var filter      = (Filter)op;
            var args        = new FilterArgs(filter);
            args.AddArg(filter.arg, DATA);
            var cx          = new ConvertContext (args, entityType, tableType);
            var result      = cx.Traverse(filter.body);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   FilterArgs       args;
        private readonly   TypeDef          entityType;
        private readonly   TableType        tableType;
        
        internal ConvertContext (FilterArgs args, TypeDef entityType, TableType tableType) {
            this.args       = args;
            this.tableType  = tableType;
            this.entityType = entityType ?? throw new InvalidOperationException(nameof(entityType));
        }
        
        private const string DblType = "::double precision";
        
        internal static string GetSqlType(ColumnType typeId) {
            switch (typeId) {
                case ColumnType.Uint8:      return "smallint";  // no byte type (0-255) available
                case ColumnType.Int16:      return "smallint";
                case ColumnType.Int32:      return "integer";
                case ColumnType.Int64:      return "bigint";
                case ColumnType.Float:      return "real";
                case ColumnType.Double:     return "double precision";
                case ColumnType.Boolean:    return "boolean";
                case ColumnType.Guid:       return "UUID";
                case ColumnType.DateTime:   return "timestamp";
                case ColumnType.BigInteger:
                case ColumnType.String:
                case ColumnType.JsonKey:
                case ColumnType.Enum:       return "text";
                case ColumnType.JsonValue:
                case ColumnType.Array:      return "JSONB"; // JSON column
                case ColumnType.Object:     return "boolean";
            }
            throw new NotSupportedException($"column type: {typeId}");
        }
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation.Type) {
                case FIELD: {
                    var field       = (Field)operation;
                    var arrayField  = args.GetArrayField(field);
                    if (tableType == TableType.Relational) {
                        if (arrayField != null) {
                            var path = ConvertPath(arrayField.array, field.name, 1, AsType.Text); 
                            return $"({path})";
                        }
                        return GetColumn(field);
                    }
                    if (arrayField != null) {
                        var path = GetArrayPath(arrayField.array, field);
                        return path;
                    } else {
                        var arg     = args.GetArg(field);
                        var path    = ConvertPath(arg, field.name, 1, AsType.Text); // todo check using GetFieldPath() instead
                        return path;
                    }
                }
                
                // --- literal --- 
                case STRING: {
                    var str     = (StringLiteral)operation;
                    var value   = SQLUtils.Escape(str.value);
                    return SQLUtils.ToSqlString(value, "(", "||", ")", "CHR");
                }
                case DOUBLE:
                    var doubleLiteral = (DoubleLiteral)operation;
                    var dbl = doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                    return $"{dbl}";
                case INT64:
                    var longLiteral = (LongLiteral)operation;
                    var lng = longLiteral.value.ToString(); 
                    return $"{lng}";
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
                    GetCasts(equal, out var leftCast, out var rightCast);
                    if (left  == "null") return $"({right} IS null)";
                    if (right == "null") return $"({left} IS null)";
                    return $"{left}{leftCast} = {right}{rightCast}";
                }
                case NOT_EQUAL: {
                    var notEqual = (NotEqual)operation;
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"({right} IS NOT null)";
                    if (right == "null") return $"({left} IS NOT null)";
                    GetCasts(notEqual, out var leftCast, out var rightCast);
                    return $"({left} is null or {right} is null or {left}{leftCast} != {right}{rightCast})";
                }
                case LESS: {
                    var lessThan = (Less)operation;
                    var left    = Traverse(lessThan.left);
                    var right   = Traverse(lessThan.right);
                    GetCasts(lessThan, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} < {right}{rightCast}";
                }
                case LESS_OR_EQUAL: {
                    var lessThanOrEqual = (LessOrEqual)operation;
                    var left    = Traverse(lessThanOrEqual.left);
                    var right   = Traverse(lessThanOrEqual.right);
                    GetCasts(lessThanOrEqual, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} <= {right}{rightCast}";
                }
                case GREATER: {
                    var greaterThan = (Greater)operation;
                    var left    = Traverse(greaterThan.left);
                    var right   = Traverse(greaterThan.right);
                    GetCasts(greaterThan, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} > {right}{rightCast}";
                }
                case GREATER_OR_EQUAL: {
                    var greaterThanOrEqual = (GreaterOrEqual)operation;
                    var left    = Traverse(greaterThanOrEqual.left);
                    var right   = Traverse(greaterThanOrEqual.right);
                    GetCasts(greaterThanOrEqual, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} >= {right}{rightCast}";
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
                    return $"ABS(({value}){DblType})";
                }
                case CEILING: {
                    var ceiling = (Ceiling)operation;
                    var value = Traverse(ceiling.value);
                    return $"ROUND(({value}+0.5){DblType})";
                }
                case FLOOR: {
                    var floor = (Floor)operation;
                    var value = Traverse(floor.value);
                    return $"ROUND(({value}-0.5){DblType})";
                }
                case EXP: {
                    var exp = (Exp)operation;
                    var value = Traverse(exp.value);
                    return $"EXP(({value}){DblType})";
                }
                case LOG: {
                    var log = (Log)operation;
                    var value = Traverse(log.value);
                    return $"LN(({value}){DblType})";
                }
                case SQRT: {
                    var sqrt = (Sqrt)operation;
                    var value = Traverse(sqrt.value);
                    return $"SQRT(({value}){DblType})";
                }
                case NEGATE: {
                    var negate = (Negate)operation;
                    var value = Traverse(negate.value);
                    return $"-(({value}){DblType})";
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
            var cx              = new ConvertContext (args, null, tableType);
            var operand         = cx.Traverse(countWhere.predicate);
            string fieldName    = Traverse(countWhere.field);
            return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
        }
        
        private string TraverseAny (Any any) {
            var arg             = any.arg;
            var arrayTable      = "je_array";
            using var scope     = args.AddArg(arg);
            using var array     = args.AddArrayField(arg, arrayTable);
            var fieldType       = GetFieldType(entityType, any.field.name);
            var cx              = new ConvertContext (args, fieldType, tableType);
            var operand         = cx.Traverse(any.predicate);
            string arrayPath    = tableType == TableType.JsonColumn ? GetFieldPath(any.field) : GetColumn(any.field);
            var result =
$@"jsonb_typeof({arrayPath}) = 'array'
  AND EXISTS(
    SELECT 1
    FROM jsonb_array_elements({arrayPath}) as {arrayTable}
    WHERE {operand}
)";
            return result;
        }
        
        private string TraverseAll (All all) {
            var arg             = all.arg;
            var arrayTable      = "je_array";
            using var scope     = args.AddArg(arg);
            using var array     = args.AddArrayField(arg, arrayTable);
            var fieldType       = GetFieldType(entityType, all.field.name);
            var cx              = new ConvertContext (args, fieldType, tableType);
            var operand         = cx.Traverse(all.predicate);
            string arrayPath    = tableType == TableType.JsonColumn ? GetFieldPath(all.field) : GetColumn(all.field);
            var result =
$@"jsonb_typeof({arrayPath}) <> 'array'
  OR NOT EXISTS(
    SELECT 1
    FROM jsonb_array_elements({arrayPath}) as {arrayTable}
    WHERE NOT ({operand})
)";
            return result;
        }
        
        private void GetCasts(BinaryBoolOp op, out string leftCast, out string rightCast) {
            leftCast    = GetCast(op.left);
            rightCast   = GetCast(op.right);
        }

        private string GetCast(Operation op) {
            if (op is Field field) {
                var fieldType   = GetFieldType(entityType, field.name);
                var type        = GetSqlType((ColumnType)fieldType.TypeId);
                if (type != "text") {
                    return "::" + type;
                }
            }
            return "";
        }
        
        private static TypeDef GetFieldType (TypeDef entityType, string path) {
            var pathFields  = path.Split('.');
            var fieldType   = entityType;
            for (int n = 1; n < pathFields.Length; n++) {
                var pathField   = pathFields[n];
                var field       = fieldType.FindField(pathField);
                if (field == null) {
                    return null;
                }
                fieldType = field.type;
            }
            return fieldType;
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                result[n] = Traverse(operands[n]);
            }
            return result;
        }
        
        internal static string ConvertPath (string arg, string path, int start, AsType asType) {
            var names   = path.Split('.');
            var count   = names.Length;
            var sb      = new StringBuilder();
            sb.Append('(');
            sb.Append(arg);
            for (int n = start; n < count; n++) {
                if (asType == AsType.Text && n == count - 1) {
                    sb.Append(" ->> '");
                } else {
                    sb.Append(" -> '");
                }
                sb.Append(names[n]);
                sb.Append('\'');
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        private string GetFieldPath(Field field) {
            var sb  = new StringBuilder();
            var arg = args.GetArg(field);    
            sb.Append(arg);
            var names   = field.name.Split('.');
            var count   = names.Length;
            for (int n = 1; n < count; n++) {
                sb.Append(" -> '");
                sb.Append(names[n]);
                sb.Append('\'');
            }
            return sb.ToString();
        }
        
        private string GetArrayPath(string arg, Field field) {
            var sb  = new StringBuilder();
            sb.Append('(');
            sb.Append(arg);
            var names   = field.name.Split('.');
            var count   = names.Length;
            for (int n = 1; n < count; n++) {
                if (n == count - 1) {
                    sb.Append(" ->> '");
                } else {
                    sb.Append(" -> '");
                }
                sb.Append(names[n]);
                sb.Append('\'');
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        private static string GetColumn(Field field) {
            var name = field.name;
            if (field.arg == name) {
                //return "$";
                throw new NotSupportedException("GetColum()");
            }
            return $"\"{name.Substring(field.arg.Length + 1)}\"";
        }
    }
    
    internal enum AsType
    {
        Text,
        JSON
    }
}
