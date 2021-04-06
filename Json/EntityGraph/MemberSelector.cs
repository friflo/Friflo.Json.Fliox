// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Friflo.Json.EntityGraph
{
    internal static class MemberSelector
    {
        internal static string PathFromExpression(Expression selector) {
            if (selector is LambdaExpression lambda) {
                var body = lambda.Body;
                var sb = new StringBuilder();
                TraceExpression(body, sb);
                return sb.ToString();
            }
            throw new NotSupportedException($"selector not supported: {selector}");
        }

        private static void TraceExpression(Expression expression, StringBuilder sb) {
            switch (expression) {
                case MemberExpression member:
                    MemberInfo memberInfo = member.Member;
                    sb.Append('.');
                    sb.Append(memberInfo.Name);
                    Type type; 
                    switch (memberInfo) {
                        case FieldInfo fieldInfo:
                            type = fieldInfo.FieldType;
                            break;
                        case PropertyInfo propertyInfo:
                            type = propertyInfo.PropertyType;
                            break;
                        default:
                            throw new NotSupportedException($"Member not supported: {member}");
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(type)) {
                        sb.Append("[*]");
                    }
                    return;
                case MethodCallExpression methodCall: 
                    var args = methodCall.Arguments;
                    for (int n = 0; n < args.Count; n++) {
                        var arg = args[n];
                        TraceExpression(arg, sb);
                    }
                    return;
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    TraceExpression(body, sb);
                    return;
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }
    }
}