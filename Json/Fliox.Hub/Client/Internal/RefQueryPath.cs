// Copyright (c) Ullrich Praetz. All rights reserved.
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
                    var parentName  = GetMemberPath(parentMember, cx);
                    var name        = QueryConverter.GetMemberName(member, cx);

                    return $"{parentName}.{name}";
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }
    }
}