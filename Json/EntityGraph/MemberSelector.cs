using System;
using System.Linq.Expressions;

namespace Friflo.Json.EntityGraph
{
    internal static class MemberSelector
    {
        internal static string PathFromExpression(Expression selector) {
            if (selector is LambdaExpression lambda) {
                if (lambda.Body is MemberExpression member) {
                    return "." + member.Member.Name;
                }
                throw new NotSupportedException($"Body not supported: {lambda.Body}");
            }
            throw new NotSupportedException($"selector not supported: {selector}");
        }
    }
}