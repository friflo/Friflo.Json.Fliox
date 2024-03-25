// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal static class ExpressionSelector
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
                    var memberInfo  = member.Member;
                    var jsonName    = AttributeUtils.GetMemberJsonName(memberInfo);
                    sb.Append('.');
                    sb.Append(jsonName);
                    var type = expression.Type;
                    if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type)) {
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