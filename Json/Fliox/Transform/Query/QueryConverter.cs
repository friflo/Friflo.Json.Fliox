// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.Mapper.Map.Val;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query
{
    public static class QueryConverter
    {
        private static readonly QueryPath DefaultQueryPath = new QueryPath();
        
        public static Operation OperationFromExpression(Expression query, QueryPath queryPath) {
            if (queryPath == null)
                queryPath = DefaultQueryPath;
            
            var cx = new QueryCx ("", "", query, queryPath);
            if (query is LambdaExpression lambda) {
                var body = lambda.Body;
                return TraceExpression(body, cx);
            }
            throw NotSupported($"query not supported: {query}", cx);
        }
        
        private static Operation TraceExpression(Expression expression, QueryCx cx) {
            switch (expression) {
                case MemberExpression member:
                    return GetMember(member, cx);
                case MethodCallExpression methodCall:
                    return OperationFromMethodCallExpression(methodCall, cx);
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    return TraceExpression(body, cx);
                case UnaryExpression unary:
                    return OperationFromUnaryExpression(unary, cx);
                case BinaryExpression binary:
                    return OperationFromBinaryExpression(binary, cx);
                case ConstantExpression constant:
                    return OperationFromConstant(constant, cx);
                default:
                    throw NotSupported($"Body not supported: {expression}", cx);
            }
        }

        private static bool IsEnumerable(Type type) {
            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        
        private static Operation GetMember(MemberExpression member, QueryCx cx) {
            switch (member.Expression) {
                case ParameterExpression _:
                    break;
                case MemberExpression parentMember:
                    var name = GetMemberName(member, cx);
                    if (name == "Count" && IsEnumerable(parentMember.Type)) {
                        var field = (Field) GetMember(parentMember, cx);
                        return new Count(field);
                    }
                    if (name == "Length" && IsEnumerable(parentMember.Type)) {
                        var field = (Field) GetMember(parentMember, cx);
                        return new Length(field);
                    }
                    break;
                case ConstantExpression constant:
                    name = GetMemberName(member, cx);
                    if (name == "Length" && constant.Type == typeof(string)) {
                        var literal = new StringLiteral(constant.Value.ToString());
                        return new Length(literal);
                    }
                    return OperationFromConstant(constant, cx);
                default:
                    throw NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
            var memberName  = cx.queryPath.GetQueryPath(member, cx);
            var paramName   = GetParameterName(member);
            return new Field(paramName + "." + memberName); // field refers to object root (.) or a lambda parameter
        }
        
        private static string GetParameterName(MemberExpression member) {
            var expression = member;
            while (expression != null) {
                var memberExpression = expression.Expression;
                if (memberExpression is ParameterExpression param) {
                    return param.Name;
                }
                expression = (MemberExpression)memberExpression;
            }
            throw new InvalidOperationException();
        }
        
        public static string GetMemberName(MemberExpression member, QueryCx cx) {
            MemberInfo memberInfo = member.Member;
            AttributeUtils.Property(memberInfo.CustomAttributes, out var customName);
            if (customName != null)
                return customName;
            
            switch (memberInfo) {
                case FieldInfo fieldInfo:
                    return fieldInfo.Name;
                case PropertyInfo propertyInfo:
                    return propertyInfo.Name;
                default:
                    throw NotSupported($"Member not supported: {member}", cx);
            }
        }

        private static Operation OperationFromMethodCallExpression(MethodCallExpression methodCall, QueryCx cx) {
            switch (methodCall.Method.Name) {
                // quantify operations
                case "Any":
                case "All":
                    return OperationFromBinaryQuantifier(methodCall, cx);
                
                // arithmetic operations
                case "Abs":
                case "Ceiling":
                case "Floor":
                case "Exp":
                case "Log":
                case "Sqrt":
                    return OperationFromUnaryArithmetic(methodCall, cx);
                
                // aggregate operations
                case "Min":
                case "Max":
                case "Sum":
                case "Count":
                case "Average":
                    return OperationFromAggregate(methodCall, cx);
                case "Contains":
                case "StartsWith":
                case "EndsWith":
                    return OperationFromBinaryCall(methodCall, cx);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }

        private static Operation OperationFromBinaryQuantifier(MethodCallExpression methodCall, QueryCx cx) {
            var args = methodCall.Arguments;
            var source      = args[0];
            var sourceOp = TraceExpression(source, cx);
            
            var predicate   = (LambdaExpression)args[1];
            string sourceField = $"{sourceOp}"; // [=>]
            var lambdaParameter = predicate.Parameters[0].Name;
            var lambdaCx = new QueryCx(lambdaParameter, cx.path + sourceField, cx.exp, cx.queryPath);
            var predicateOp = (FilterOperation)TraceExpression(predicate, lambdaCx);
            
            switch (methodCall.Method.Name) {
                case "Any":     return new Any(new Field(sourceField), lambdaParameter, predicateOp);
                case "All":     return new All(new Field(sourceField), lambdaParameter, predicateOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromUnaryArithmetic(MethodCallExpression methodCall, QueryCx cx) {
            var value = methodCall.Arguments[0];
            var valueOp = TraceExpression(value, cx);
            switch (methodCall.Method.Name) {
                // --- arithmetic operations
                case "Abs":     return new Abs      (valueOp);
                case "Ceiling": return new Ceiling  (valueOp);
                case "Floor":   return new Floor    (valueOp);
                case "Exp":     return new Exp      (valueOp);
                case "Log":     return new Log      (valueOp);
                case "Sqrt":    return new Sqrt     (valueOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromAggregate(MethodCallExpression methodCall, QueryCx cx) {
            var     args        = methodCall.Arguments;
            var     source      = args[0];
            var     sourceOp    = TraceExpression(source, cx);
            string  sourceField = $"{sourceOp}"; // [=>]
            
            switch (methodCall.Method.Name) {
                case "Count":
                    if (args.Count >= 2) {
                        var predicate   = (LambdaExpression)args[1];
                        var lambdaParameter = predicate.Parameters[0].Name;
                        var lambdaCxCount = new QueryCx(lambdaParameter, cx.path, cx.exp, cx.queryPath);
                        var predicateOp = (FilterOperation)TraceExpression(predicate, lambdaCxCount);
                        return new CountWhere(new Field(sourceField), lambdaParameter, predicateOp);
                    }
                    return new Count(new Field(sourceField));
                case "Min":
                case "Max":
                case "Sum":
                case "Average":
                    var lambdaCx = new QueryCx(cx.parameter, cx.path + sourceField, cx.exp, cx.queryPath);
                    return OperationFromBinaryAggregate(methodCall, lambdaCx);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromBinaryCall(MethodCallExpression methodCall, QueryCx cx) {
            var leftOp  = TraceExpression(methodCall.Object,       cx);
            var rightOp = TraceExpression(methodCall.Arguments[0], cx);
            switch (methodCall.Method.Name) {
                // --- binary comparison operations
                case "Contains":            return new Contains     (leftOp, rightOp);
                case "StartsWith":          return new StartsWith   (leftOp, rightOp);
                case "EndsWith":            return new EndsWith     (leftOp, rightOp);

                default:
                    throw NotSupported($"Method not supported. method: {methodCall}", cx);
            }
        }
        
        private static Operation OperationFromBinaryAggregate(MethodCallExpression methodCall, QueryCx cx) {
            var predicate   = (LambdaExpression)methodCall.Arguments[1];
            string sourceField = $"{cx.path}";
            var lambdaParameter = predicate.Parameters[0].Name;
            var lambdaCx = new QueryCx(lambdaParameter, cx.path, cx.exp, cx.queryPath);
            var valueOp = TraceExpression(predicate, lambdaCx);
            
            switch (methodCall.Method.Name) {
                case "Min":     return new Min      (new Field(sourceField), lambdaParameter, valueOp);
                case "Max":     return new Max      (new Field(sourceField), lambdaParameter, valueOp);
                case "Sum":     return new Sum      (new Field(sourceField), lambdaParameter, valueOp);
                case "Average": return new Average  (new Field(sourceField), lambdaParameter, valueOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }

        private static Operation OperationFromUnaryExpression(UnaryExpression unary, QueryCx cx) {
            var operand = TraceExpression(unary.Operand, cx);
            switch (unary.NodeType) {
                case ExpressionType.Not:            return new Not((FilterOperation)operand);
                case ExpressionType.Negate:         return new Negate(operand);
                case ExpressionType.Convert:
                    return OperationFromConvert(unary, cx);
                default:
                    throw NotSupported($"UnaryExpression not supported: {unary}", cx);
            }
        }

        private static Operation OperationFromConvert(UnaryExpression unary, QueryCx cx) {
            var type = unary.Operand.NodeType;
            switch (type) {
                /*
                case ExpressionType.Constant:
                case ExpressionType.Call:
                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Negate:
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                    return TraceExpression(unary.Operand, cx); */
                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)unary.Operand;
                    return GetMember(member, cx);
                default:
                    return TraceExpression(unary.Operand, cx);
                    // throw NotSupported($"Convert Operand not supported. operand: {type}", cx);
            }
        }

        private static Operation OperationFromBinaryExpression(BinaryExpression binary, QueryCx cx) {
            var leftOp  = TraceExpression(binary.Left,  cx);
            var rightOp = TraceExpression(binary.Right, cx);
            switch (binary.NodeType) {
                // --- binary comparison operations
                case ExpressionType.Equal:              return new Equal            (leftOp, rightOp);
                case ExpressionType.NotEqual:           return new NotEqual         (leftOp, rightOp);
                case ExpressionType.LessThan:           return new Less             (leftOp, rightOp);
                case ExpressionType.LessThanOrEqual:    return new LessOrEqual      (leftOp, rightOp);
                case ExpressionType.GreaterThan:        return new Greater          (leftOp, rightOp);
                case ExpressionType.GreaterThanOrEqual: return new GreaterOrEqual   (leftOp, rightOp);
                
                // --- group operations:
                case ExpressionType.OrElse:             return new Or (new List<FilterOperation> {(FilterOperation)leftOp, (FilterOperation)rightOp});
                case ExpressionType.AndAlso:            return new And(new List<FilterOperation> {(FilterOperation)leftOp, (FilterOperation)rightOp});
                
                // --- binary arithmetic operations
                case ExpressionType.Add:                return new Add                  (leftOp, rightOp);
                case ExpressionType.Subtract:           return new Subtract             (leftOp, rightOp);
                case ExpressionType.Multiply:           return new Multiply             (leftOp, rightOp);
                case ExpressionType.Divide:             return new Divide               (leftOp, rightOp);
                case ExpressionType.Modulo:             return new Modulo               (leftOp, rightOp);

                default:
                    throw NotSupported($"Method not supported. method: {binary}", cx);
            }
        }
        
        private static Operation OperationFromConstant(ConstantExpression constant, QueryCx cx) {
            object  value       = constant.Value;
            Type    type        = constant.Type;
            
            // is local variable used in expression? A DisplayClass is generated for them
            if (type.IsDefined (typeof (CompilerGeneratedAttribute), false)) {
                var fields  = type.GetFields();
                var field   = fields[0];
                value       = field.GetValue(value);
                type        = field.FieldType;
            }
            var operation   =  OperationFromValue(value, type);    
            
            if (operation == null)
                throw NotSupported($"Constant not supported: {constant}, type: {type.FullName}", cx);
            return operation;
        }

        private static Operation OperationFromValue(object value, Type type)
        {
            if (type == typeof(string))     return new StringLiteral((string)   value);
            
            // --- floating point
            if (type == typeof(double))     return new DoubleLiteral((double)   value);
            if (type == typeof(float))      return new DoubleLiteral((float)    value);
            
            // --- integral
            if (type == typeof(long))       return new LongLiteral((long)       value);
            if (type == typeof(int))        return new LongLiteral((int)        value);
            if (type == typeof(short))      return new LongLiteral((short)      value);
            if (type == typeof(byte))       return new LongLiteral((byte)       value);
            
            // --- bool
            if (type == typeof(bool)) {
                if ((bool) value)
                    return Operation.FilterTrue;
                return Operation.FilterFalse;
            }
            
            // --- null
            if (type == typeof(object) && value == null)
                return new NullLiteral();
            
            if (type == typeof(DateTime)) {
                var str = DateTimeMapper.ToRFC_3339((DateTime)value);
                return new StringLiteral(str);
            }
            return null;
        }

        public static Exception NotSupported(string message, QueryCx cx) {
            return new NotSupportedException($"{message}, expression: {cx.exp}");
        }
    }

    public sealed class QueryCx
    {
        internal readonly   string      parameter;
        internal readonly   string      path;
        internal readonly   Expression  exp;
        internal readonly   QueryPath   queryPath;

        internal QueryCx(string parameter, string path, Expression exp, QueryPath queryPath) {
            this.path       = path;
            this.exp        = exp;
            this.parameter  = parameter;
            this.queryPath  = queryPath;
        }
    }

    public class QueryPath {
        public virtual string GetQueryPath(MemberExpression member, QueryCx cx) {
            switch (member.Expression) {
                case ParameterExpression _:
                    return QueryConverter.GetMemberName(member, cx);
                case MemberExpression parentMember:
                    var name        = QueryConverter.GetMemberName(member, cx);
                    var parentName  = GetQueryPath(parentMember, cx);
                    return $"{parentName}.{name}";
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }       
    }
}