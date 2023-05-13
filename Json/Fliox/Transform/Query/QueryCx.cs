// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                    var name        = QueryConverter.GetMemberName(member, cx);
                    var parentName  = GetMemberPath(parentMember, cx);
                    return $"{parentName}.{name}";
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }       
    }
}