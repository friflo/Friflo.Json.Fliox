// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform.Query;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal sealed class RefQueryPath : QueryPath
    {
        public override string GetMemberPath(MemberExpression member, LambdaCx cx) {
            switch (member.Expression) {
                case ParameterExpression _:
                    return QueryConverter.GetMemberName(member, cx);
                case MemberExpression parentMember:
                    return GetMemberExpressionPath(member, parentMember, cx);
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }
    }
}