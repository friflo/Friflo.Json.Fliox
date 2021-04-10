using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Friflo.Json.EntityGraph.Filter
{
    internal static class QueryConverter
    {
        public static Operator OperatorFromExpression(Expression query) {
            if (query is LambdaExpression lambda) {
                var body = lambda.Body;
                var op = TraceExpression(body);
                return op;
            }
            throw new NotSupportedException($"query not supported: {query}");
        }
        
        private static Operator TraceExpression(Expression expression) {
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
                    return new Field(name);
                case MethodCallExpression methodCall:
                    return OperatorFromMethodCallExpression(methodCall);
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    return TraceExpression(body);
                case UnaryExpression unary:
                    var op = OperatorFromUnaryExpression(unary);
                    return op;
                case BinaryExpression binary:
                    op = OperatorFromBinaryExpression(binary);
                    return op;
                case ConstantExpression constant:
                    return OperatorFromConstant(constant);
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }
        
        private static Operator OperatorFromMethodCallExpression(MethodCallExpression methodCall) {
            var args = methodCall.Arguments;
            var opArgs = new List<Operator>();
            for (int n = 0; n < args.Count; n++) {
                var arg = args[n];
                var boolOp = TraceExpression(arg); 
                opArgs.Add(boolOp);
            }

            switch (methodCall.Method.Name) {
                case "Any":
                    return new Any((BoolOp)opArgs[1]);
                case "All":
                    return new All((BoolOp)opArgs[1]);
                case "Abs":
                    return new Abs(opArgs[0]);
                case "Ceiling":
                    return new Ceiling(opArgs[0]);
                case "Floor":
                    return new Floor(opArgs[0]);
                case "Exp":
                    return new Exp(opArgs[0]);
                case "Log":
                    return new Log(opArgs[0]);
                case "Sqrt":
                    return new Sqrt(opArgs[0]);
                default:
                    throw new NotSupportedException($"MethodCallExpression not supported: {methodCall}");
            }
        }

        private static Operator OperatorFromUnaryExpression(UnaryExpression unary) {
            var operand = TraceExpression(unary.Operand);
            switch (unary.NodeType) {
                case ExpressionType.Not:
                    return new Not((BoolOp)operand);
                case ExpressionType.Convert:
                    return operand;
                default:
                    throw new NotSupportedException($"UnaryExpression not supported: {unary}");
            }
        }

        private static Operator OperatorFromBinaryExpression(BinaryExpression binary) {
            var leftOp  = TraceExpression(binary.Left);
            var rightOp = TraceExpression(binary.Right);
            switch (binary.NodeType) {
                // --- binary comparison operators
                case ExpressionType.Equal:
                    return new Equal(leftOp, rightOp);
                case ExpressionType.NotEqual:
                    return new NotEqual(leftOp, rightOp);
                case ExpressionType.LessThan:
                    return new LessThan(leftOp, rightOp);
                case ExpressionType.LessThanOrEqual:
                    return new LessThanOrEqual(leftOp, rightOp);
                case ExpressionType.GreaterThan:
                    return new GreaterThan(leftOp, rightOp);
                case ExpressionType.GreaterThanOrEqual:
                    return new GreaterThanOrEqual(leftOp, rightOp);
                
                // --- group operator:
                case ExpressionType.OrElse:
                    return new Or(new List<BoolOp>{(BoolOp)leftOp, (BoolOp)rightOp});
                case ExpressionType.AndAlso:
                    return new And(new List<BoolOp>{(BoolOp)leftOp, (BoolOp)rightOp});
                
                // --- binary arithmetic operators
                case ExpressionType.Add:
                    return new Add(leftOp, rightOp);
                case ExpressionType.Subtract:
                    return new Subtract(leftOp, rightOp);
                case ExpressionType.Multiply:
                    return new Multiply(leftOp, rightOp);
                case ExpressionType.Divide:
                    return new Divide(leftOp, rightOp);

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
}