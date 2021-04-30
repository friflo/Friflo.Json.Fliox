// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Friflo.Json.Flow.Transform.Query;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal class GraphMemberAccessor : MemberAccessor
    {
        public override string GetMemberPath(MemberExpression member, QueryCx cx) {
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