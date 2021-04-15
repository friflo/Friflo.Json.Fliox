// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Query
{
    internal static class QueryConverter
    {
        public static Operator OperatorFromExpression(Expression query) {
            var cx = new QueryCx ("", query);
            if (query is LambdaExpression lambda) {
                var body = lambda.Body;
                return TraceExpression(body, cx);
            }
            throw NotSupported($"query not supported: {query}", cx);
        }
        
        private static Operator TraceExpression(Expression expression, QueryCx cx) {
            switch (expression) {
                case MemberExpression member:
                    string name = GetMemberName(member, cx);
                    return new Field(cx.path + "." + name);
                case MethodCallExpression methodCall:
                    return OperatorFromMethodCallExpression(methodCall, cx);
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    return TraceExpression(body, cx);
                case UnaryExpression unary:
                    return OperatorFromUnaryExpression(unary, cx);
                case BinaryExpression binary:
                    return OperatorFromBinaryExpression(binary, cx);
                case ConstantExpression constant:
                    return OperatorFromConstant(constant, cx);
                default:
                    throw NotSupported($"Body not supported: {expression}", cx);
            }
        }

        private static string GetMemberName(MemberExpression member, QueryCx cx) {
            MemberInfo memberInfo = member.Member;
            switch (memberInfo) {
                case FieldInfo fieldInfo:           return fieldInfo.Name;
                case PropertyInfo propertyInfo:     return propertyInfo.Name;
                default:
                    throw NotSupported($"Member not supported: {member}", cx);
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
                    return OperatorFromAggregate(methodCall, cx);
                
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }

        private static Operator OperatorFromBinaryQuantifier(MethodCallExpression methodCall, QueryCx cx) {
            var args = methodCall.Arguments;
            var source      = args[0];
            var sourceOp = TraceExpression(source, cx);
            
            var predicate   = args[1];
            string sourceField = $"{sourceOp}[@]";
            var lambdaCx = new QueryCx(cx.path + sourceField, cx.exp);
            var predicateOp = (BoolOp)TraceExpression(predicate, lambdaCx);
            
            switch (methodCall.Method.Name) {
                case "Any":     return new Any(new Field(sourceField), predicateOp);
                case "All":     return new All(new Field(sourceField), predicateOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operator OperatorFromUnaryArithmetic(MethodCallExpression methodCall, QueryCx cx) {
            var value = methodCall.Arguments[0];
            var valueOp = TraceExpression(value, cx);
            switch (methodCall.Method.Name) {
                // --- arithmetic operators
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
        
        private static Operator OperatorFromAggregate(MethodCallExpression methodCall, QueryCx cx) {
            var     args        = methodCall.Arguments;
            var     source      = args[0];
            var     sourceOp    = TraceExpression(source, cx);
            string  sourceField = $"{sourceOp}[@]";
            
            switch (methodCall.Method.Name) {
                case "Count":
                    return new Count(new Field(sourceField));
                case "Min":
                case "Max":
                case "Sum":
                case "Average":
                    var lambdaCx = new QueryCx(cx.path + sourceField, cx.exp);
                    return OperatorFromBinaryAggregate(methodCall, lambdaCx);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }
        
        private static Operator OperatorFromBinaryAggregate(MethodCallExpression methodCall, QueryCx cx) {
            var predicate   = methodCall.Arguments[1];
            var valueOp = TraceExpression(predicate, cx);
            
            switch (methodCall.Method.Name) {
                case "Min":     return new Min      (valueOp);
                case "Max":     return new Max      (valueOp);
                case "Sum":     return new Sum      (valueOp);
                case "Average": return new Average  (valueOp);
                default:
                    throw NotSupported($"MethodCallExpression not supported: {methodCall}", cx);
            }
        }

        private static Operator OperatorFromUnaryExpression(UnaryExpression unary, QueryCx cx) {
            var operand = TraceExpression(unary.Operand, cx);
            switch (unary.NodeType) {
                case ExpressionType.Not:            return new Not((BoolOp)operand);
                case ExpressionType.Negate:         return new Negate(operand);
                case ExpressionType.Convert:
                    return OperatorFromConvert(unary, cx);
                default:
                    throw NotSupported($"UnaryExpression not supported: {unary}", cx);
            }
        }

        private static Operator OperatorFromConvert(UnaryExpression unary, QueryCx cx) {
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
                    return OperatorFromMember(member, cx);
                default:
                    return TraceExpression(unary.Operand, cx);
                    // throw NotSupported($"Convert Operand not supported. operand: {type}", cx);
            }
        }

        private static Operator OperatorFromMember(MemberExpression member, QueryCx cx) {
            var name = GetMemberName(member, cx);
            var field = (Field)TraceExpression(member.Expression, cx);
            switch (name) {
                case "Count":
                    field.field = field.field + "[@]";
                    return new Count(field);
            }
            throw NotSupported($"Convert MemberAccess not supported. member: {member}", cx);
        }

        private static Operator OperatorFromBinaryExpression(BinaryExpression binary, QueryCx cx) {
            var leftOp  = TraceExpression(binary.Left,  cx);
            var rightOp = TraceExpression(binary.Right, cx);
            switch (binary.NodeType) {
                // --- binary comparison operators
                case ExpressionType.Equal:              return new Equal                (leftOp, rightOp);
                case ExpressionType.NotEqual:           return new NotEqual             (leftOp, rightOp);
                case ExpressionType.LessThan:           return new LessThan             (leftOp, rightOp);
                case ExpressionType.LessThanOrEqual:    return new LessThanOrEqual      (leftOp, rightOp);
                case ExpressionType.GreaterThan:        return new GreaterThan          (leftOp, rightOp);
                case ExpressionType.GreaterThanOrEqual: return new GreaterThanOrEqual   (leftOp, rightOp);
                
                // --- group operator:
                case ExpressionType.OrElse:             return new Or(new List<BoolOp>  {(BoolOp)leftOp, (BoolOp)rightOp});
                case ExpressionType.AndAlso:            return new And(new List<BoolOp> {(BoolOp)leftOp, (BoolOp)rightOp});
                
                // --- binary arithmetic operators
                case ExpressionType.Add:                return new Add                  (leftOp, rightOp);
                case ExpressionType.Subtract:           return new Subtract             (leftOp, rightOp);
                case ExpressionType.Multiply:           return new Multiply             (leftOp, rightOp);
                case ExpressionType.Divide:             return new Divide               (leftOp, rightOp);

                default:
                    throw NotSupported($"Method not supported. method: {binary}", cx);
            }
        }

        private static Operator OperatorFromConstant(ConstantExpression constant, QueryCx cx) {
            Type type = constant.Type;
            object value = constant.Value;
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
            if (type == typeof(bool))       return new BoolLiteral((bool)       value);
            
            // --- null
            if (type == typeof(object) && value == null)
                return new NullLiteral();

            throw NotSupported($"Constant not supported: {constant}", cx);
        }

        static Exception NotSupported(string message, QueryCx cx) {
            return new NotSupportedException($"{message}, expression: {cx.exp}");
        }
    }

    internal class QueryCx
    {
        internal readonly string        path;
        internal readonly Expression    exp;

        internal QueryCx(string path, Expression exp) {
            this.path = path;
            this.exp  = exp;
        }
    }
}