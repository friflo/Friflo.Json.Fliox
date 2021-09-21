// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Friflo.Json.Fliox.Transform.Query;

namespace Friflo.Json.Fliox.DB.Client.Internal
{
    internal class RefQueryPath : QueryPath
    {
        public override string GetQueryPath(MemberExpression member, QueryCx cx) {
            switch (member.Expression) {
                case ParameterExpression _:
                    return QueryConverter.GetMemberName(member, cx);
                case MemberExpression parentMember:
                    var type = parentMember.Type;
                    var isRef = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Ref<,>);
                    var parentName  = GetQueryPath(parentMember, cx);
                    var name        = QueryConverter.GetMemberName(member, cx);
                    if (isRef) {
                        if (name == "Key")
                            return parentName;
                        throw QueryConverter.NotSupported($"Query using Ref<>.Entity intentionally not supported. Only Ref<>.id is valid: {member}", cx); 
                    }
                    return $"{parentName}.{name}";
                default:
                    throw QueryConverter.NotSupported($"MemberExpression.Expression not supported: {member}", cx); 
            }
        }
    }
}