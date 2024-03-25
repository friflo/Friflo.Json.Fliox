// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace Friflo.Json.Fliox.Transform.Query
{
    internal sealed class QueryCx
    {
        internal readonly   Expression      exp;
        internal readonly   QueryPath       queryPath;

        internal QueryCx(Expression exp, QueryPath queryPath) {
            this.exp        = exp;
            this.queryPath  = queryPath;
        }
    }

    public sealed class LambdaCx
    {
        internal readonly   string      parameter;
        internal readonly   string      path;
        internal readonly   QueryCx     query;

        internal LambdaCx(string parameter, string path, QueryCx query) {
            if (string.IsNullOrEmpty(parameter)) throw new ArgumentException(nameof(parameter));
            this.parameter  = parameter;
            this.path       = path;
            this.query      = query;
        }
    }

    public class QueryPath {
        public virtual string GetMemberPath(MemberExpression member, LambdaCx cx) {
            switch (member.Expression) {
                case ParameterExpression _:
                    return QueryConverter.GetMemberName(member, cx);
                case MemberExpression parentMember:
                    return GetMemberExpressionPath(member, parentMember, cx);
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }
        
        protected string GetMemberExpressionPath(MemberExpression member, MemberExpression parentMember, LambdaCx cx) {
            var parentName      = GetMemberPath(parentMember, cx);
            var memberInfo      = member.Member;
            var declaringType   = memberInfo.DeclaringType;
            if (declaringType != null && Nullable.GetUnderlyingType(declaringType) != null && memberInfo.Name == "Value") {
                return parentName;
            }
            var name        = QueryConverter.GetMemberName(member, cx);
            return $"{parentName}.{name}";
        }
    }
}