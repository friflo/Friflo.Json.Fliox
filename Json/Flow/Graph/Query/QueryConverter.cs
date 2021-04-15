// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Query
{
    internal static class QueryConverter
    {
        public static Operator OperatorFromExpression(Expression query) {
            if (query is LambdaExpression lambda) {
                var body = lambda.Body;
                var cx = new QueryCx ("");
                var op = TraceExpression(body, cx);
                return op;
            }
            throw new NotSupportedException($"query not supported: {query}");
        }
        
        private static Operator TraceExpression(Expression expression, QueryCx cx) {
            switch (expression) {
                case MemberExpression member:
                    MemberInfo memberInfo = member.Member;
                    string name;
                    switch (memberInfo) {
                        case FieldInfo fieldInfo:
                            name = fieldInfo.Name;
                            break;
                        case PropertyInfo propertyInfo:
                            name = propertyInfo.Name;
                            break;
                        default:
                            throw new NotSupportedException($"Member not supported: {member}");
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(expression.Type)) {
                        // isArraySelector = true;
                    }
                    return new Field(cx.path + "." + name);
                case MethodCallExpression methodCall:
                    return OperatorFromMethodCallExpression(methodCall, cx);
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    return TraceExpression(body, cx);
                case UnaryExpression unary:
                    var op = OperatorFromUnaryExpression(unary, cx);
                    return op;
                case BinaryExpression binary:
                    op = OperatorFromBinaryExpression(binary, cx);
                    return op;
                case ConstantExpression constant:
                    return OperatorFromConstant(constant);
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }

        private static Operator OperatorFromMethodCallExpression(MethodCallExpression methodCall, QueryCx cx) {
            switch (methodCall.Method.Name) {
                // quantify operators
                case "Any":
                case "All":
                    return OperatorFromBinaryQuantifier(methodCall, cx);
                
                // arithmetic operators
                case "Abs":
                case "Ceiling":
                case "Floor":
                case "Exp":
                case "Log":
                case "Sqrt":
                    return OperatorFromUnaryArithmetic(methodCall, cx);
                
                // aggregate operators
                case "Min":
                case "Max":
                case "Sum":
                case "Count":
                case "Average":
                    return OperatorFromUnaryAggregate(methodCall, cx);
                
                default:
                    throw new NotSupportedException($"MethodCallExpression not supported: {methodCall}");
            }
        }

        private static Operator OperatorFromBinaryQuantifier(MethodCallExpression methodCall, QueryCx cx) {
            var args = methodCall.Arguments;
            var source      = args[0];
            var predicate   = args[1];
            var sourceOp = TraceExpression(source, cx);
            
            string sourceField = $"{sourceOp}[@]";
            var lambdaCx = new QueryCx(cx.path + sourceField);
            var predicateOp = (BoolOp)TraceExpression(predicate, lambdaCx);
            
            switch (methodCall.Method.Name) {
                case "Any":     return new Any(new Field(sourceField), predicateOp);
                case "All":     return new All(new Field(sourceField), predicateOp);
                default:
                    throw new NotSupportedException($"MethodCallExpression not supported: {methodCall}");
            }
        }
        
        private static Operator OperatorFromUnaryArithmetic(MethodCallExpression methodCall, QueryCx cx) {
            var value = methodCall.Arguments[0];
            var valueOp = TraceExpression(value, cx);
            switch (methodCall.Method.Name) {
                // --- arithmetic operators
                case "Abs":     return new Abs(valueOp);
                case "Ceiling": return new Ceiling(valueOp);
                case "Floor":   return new Floor(valueOp);
                case "Exp":     return new Exp(valueOp);
                case "Log":     return new Log(valueOp);
                case "Sqrt":    return new Sqrt(valueOp);
                default:
                    throw new NotSupportedException($"MethodCallExpression not supported: {methodCall}");
            }
        }
        
        private static Operator OperatorFromUnaryAggregate(MethodCallExpression methodCall, QueryCx cx) {
            var value = methodCall.Arguments[0];
            var valueOp = TraceExpression(value, cx);
            switch (methodCall.Method.Name) {
                case "Min":     return new Min(valueOp);
                case "Max":     return new Max(valueOp);
                case "Sum":     return new Sum(valueOp);
                case "Average": return new Average(valueOp);
                case "Count":   return new Count(valueOp);
                default:
                    throw new NotSupportedException($"MethodCallExpression not supported: {methodCall}");
            }
        }

        private static Operator OperatorFromUnaryExpression(UnaryExpression unary, QueryCx cx) {
            var operand = TraceExpression(unary.Operand, cx);
            switch (unary.NodeType) {
                case ExpressionType.Not:        return new Not((BoolOp)operand);
                case ExpressionType.Convert:    return operand;
                default:
                    throw new NotSupportedException($"UnaryExpression not supported: {unary}");
            }
        }

        private static Operator OperatorFromBinaryExpression(BinaryExpression binary, QueryCx cx) {
            var leftOp  = TraceExpression(binary.Left,  cx);
            var rightOp = TraceExpression(binary.Right, cx);
            switch (binary.NodeType) {
                // --- binary comparison operators
                case ExpressionType.Equal:              return new Equal(leftOp, rightOp);
                case ExpressionType.NotEqual:           return new NotEqual(leftOp, rightOp);
                case ExpressionType.LessThan:           return new LessThan(leftOp, rightOp);
                case ExpressionType.LessThanOrEqual:    return new LessThanOrEqual(leftOp, rightOp);
                case ExpressionType.GreaterThan:        return new GreaterThan(leftOp, rightOp);
                case ExpressionType.GreaterThanOrEqual: return new GreaterThanOrEqual(leftOp, rightOp);
                
                // --- group operator:
                case ExpressionType.OrElse:             return new Or(new List<BoolOp>{(BoolOp)leftOp, (BoolOp)rightOp});
                case ExpressionType.AndAlso:            return new And(new List<BoolOp>{(BoolOp)leftOp, (BoolOp)rightOp});
                
                // --- binary arithmetic operators
                case ExpressionType.Add:                return new Add(leftOp, rightOp);
                case ExpressionType.Subtract:           return new Subtract(leftOp, rightOp);
                case ExpressionType.Multiply:           return new Multiply(leftOp, rightOp);
                case ExpressionType.Divide:             return new Divide(leftOp, rightOp);

                default:
                    throw new NotSupportedException($"Method not supported. method: {binary}");
            }
        }

        private static Operator OperatorFromConstant(ConstantExpression constant) {
            Type type = constant.Type;
            object value = constant.Value;
            if (type == typeof(string))
                return new StringLiteral((string)   value);
            
            if (type == typeof(double))
                return new DoubleLiteral((double)   value);
            if (type == typeof(float))
                return new DoubleLiteral((float)    value);
            
            if (type == typeof(long))
                return new LongLiteral((long)       value);
            if (type == typeof(int))
                return new LongLiteral((int)        value);
            if (type == typeof(short))
                return new LongLiteral((short)      value);
            if (type == typeof(byte))
                return new LongLiteral((byte)       value);
            
            if (type == typeof(bool))
                return new BoolLiteral((bool)    value);
            
            if (type == typeof(object) && value == null)
                return new NullLiteral();

            throw new NotSupportedException($"Constant not supported: {constant}");
        }
    }

    internal class QueryCx
    {
        internal readonly string path;

        internal QueryCx(string path) {
            this.path = path;
        }
    }
}