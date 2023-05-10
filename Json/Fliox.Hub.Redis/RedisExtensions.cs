// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable UnusedParameter.Local
namespace Friflo.Json.Fliox.Hub.Redis
{
    public static class RedisExtensions
    {
        public static string RedisFilter(this FilterOperation op) {
            var filter      = (Filter)op;
            var args        = new FilterArgs(filter);
            args.AddArg(filter.arg, "data");
            var cx          = new ConvertContext (args);
            var result      = cx.Traverse(filter.body);
            return result;
        }
    }
    
    internal sealed class ConvertContext {

        private readonly   FilterArgs   args;
        
        internal ConvertContext (FilterArgs args) {
            this.args = args;
        }
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field: {
                    throw new NotImplementedException("Field");
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
                    // e.g. WHERE json_extract(data,'$.int32') is null || JSON_TYPE(json_extract(data,'$.int32')) = 'NULL'
                    if (left  == "null") return $"({right} is null)";
                    if (right == "null") return $"({left} is null)";
                    return $"{left} = {right}";
                }
                case NotEqual notEqual: {
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    // e.g WHERE json_extract(data,'$.int32') is not null && JSON_TYPE(json_extract(data,'$.int32')) != 'NULL'
                    if (left  == "null") return $"({right} is not null)";
                    if (right == "null") return $"({left} is not null)";
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
            throw new NotImplementedException(nameof(TraverseCount));
        }
        
        private string TraverseAny (Any any) {
            throw new NotImplementedException(nameof(TraverseAny));
        }
        
        private string TraverseAll (All all) {
            throw new NotImplementedException(nameof(TraverseAll));
        }
        
        private string ToBoolean(string operand) {
            return operand;
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
        
        private static string UnString(string value) {
            if (value[0] == '\'') {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}