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
                    Type type; 
                    string name;
                    switch (memberInfo) {
                        case FieldInfo fieldInfo:
                            type = fieldInfo.FieldType;
                            name = fieldInfo.Name;
                            break;
                        case PropertyInfo propertyInfo:
                            type = propertyInfo.PropertyType;
                            name = propertyInfo.Name;
                            break;
                        default:
                            throw new NotSupportedException($"Member not supported: {member}");
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(type)) {
                        // sb.Append("[*]");
                        // isArraySelector = true;
                    }
                    if (expression.Type == typeof(string)) {
                        return new Field(name);
                    }
                    throw new NotSupportedException($"Constant not supported: {expression}");
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
                    var method = binary.Method;
                    var op = OperatorFromBinaryMethod(method, binary.Left, binary.Right);
                    return op;
                case ConstantExpression constant:
                    if (constant.Type == typeof(string)) {
                        return new StringLiteral((string)constant.Value);
                    }
                    throw new NotSupportedException($"Constant not supported: {constant}");
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }

        private static Operator OperatorFromBinaryMethod(MethodInfo method, Expression left, Expression right) {
            switch (method.Name) {
                case "op_Equality":
                    var leftOp  = TraceExpression(left);
                    var rightOp = TraceExpression(right);
                    return new Equals(leftOp, rightOp);
                default:
                    throw new NotSupportedException($"Method not supported. method: {method}, left {left}, right: {right}");
            }
        }

    }
}