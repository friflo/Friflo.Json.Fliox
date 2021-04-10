using System;
using System.Collections;
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
                    var args = methodCall.Arguments;
                    for (int n = 0; n < args.Count; n++) {
                        var arg = args[n];
                        TraceExpression(arg);
                    }
                    return null;
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    TraceExpression(body);
                    return null;
                case BinaryExpression binary:
                    var op = OperatorFromBinaryMethod(binary, binary.Left, binary.Right);
                    return op;
                case ConstantExpression constant:
                    return OperatorFromConstant(constant);
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }

        private static Operator OperatorFromBinaryMethod(BinaryExpression binary, Expression left, Expression right) {
            var leftOp  = TraceExpression(left);
            var rightOp = TraceExpression(right);
            switch (binary.NodeType) {
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

                default:
                    throw new NotSupportedException($"Method not supported. method: {binary.NodeType}, left {left}, right: {right}");
            }
        }

        private static Operator OperatorFromConstant(ConstantExpression constant) {
            var type = constant.Type;
            if (type == typeof(string))
                return new StringLiteral((string)constant.Value);
            
            if (type == typeof(long))
                return new NumberLiteral((long)constant.Value);
            if (type == typeof(int))
                return new NumberLiteral((int)constant.Value);
            if (type == typeof(short))
                return new NumberLiteral((short)constant.Value);
            if (type == typeof(byte))
                return new NumberLiteral((byte)constant.Value);

            throw new NotSupportedException($"Constant not supported: {constant}");
        }

    }
}