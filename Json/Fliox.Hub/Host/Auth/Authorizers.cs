using System.Collections.Generic;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    internal static class AuthorizerUtils
    {
        internal static Authorizer ToAuthorizer(IReadOnlyList<Authorizer> authorizers) {
            if (authorizers == null)
                return AuthorizeDeny.Instance;
            if (authorizers.Count == 0)
                return AuthorizeDeny.Instance;
            return TrimAny(authorizers);
        }

        private static Authorizer TrimAny(IReadOnlyList<Authorizer> list) {
            while (true) {
                if (list.Count == 1) {
                    var single = list[0];
                    if (single is AuthorizeAny any) {
                        list = any.list;
                        continue;
                    }
                    return single;
                }
                return new AuthorizeAny(list);
            }
        }
    }
}