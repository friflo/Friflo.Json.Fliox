// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal static class MemberSelector
    {
        internal static string PathFromExpression(Expression selector, out bool isArraySelector) {
            isArraySelector = false;
            if (selector is LambdaExpression lambda) {
                var body = lambda.Body;
                var sb = new StringBuilder();
                TraceExpression(body, sb, ref isArraySelector);
                return sb.ToString();
            }
            throw new NotSupportedException($"selector not supported: {selector}");
        }

        private static void TraceExpression(Expression expression, StringBuilder sb, ref bool isArraySelector) {
            switch (expression) {
                case MemberExpression member:
                    MemberInfo memberInfo = member.Member;
                    sb.Append('.');
                    sb.Append(memberInfo.Name);
                    if (typeof(IEnumerable).IsAssignableFrom(expression.Type)) {
                        sb.Append("[*]");
                        isArraySelector = true;
                    }
                    return;
                case MethodCallExpression methodCall: 
                    var args = methodCall.Arguments;
                    for (int n = 0; n < args.Count; n++) {
                        var arg = args[n];
                        TraceExpression(arg, sb, ref isArraySelector);
                    }
                    return;
                case LambdaExpression lambda: 
                    var body = lambda.Body;
                    TraceExpression(body, sb, ref isArraySelector);
                    return;
                default:
                    throw new NotSupportedException($"Body not supported: {expression}");
            }
        }
    }
}