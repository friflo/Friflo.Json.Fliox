using System;
using System.Collections.Generic;
using System.Linq;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    internal readonly struct Authorizers
    {
        internal readonly IReadOnlyList<Authorizer> list;
        
        internal Authorizers(Authorizer authorizer) {
            if (authorizer == null) {
                list = null;
                return;
            }
            list = new [] { authorizer };
        }
        
        internal Authorizers(IReadOnlyList<Authorizer> authorizers) {
            foreach (var authorizer in authorizers) {
                if (authorizer == null) throw new NullReferenceException(nameof(authorizer));
            }
            list = authorizers.ToArray();
        }
        
        internal bool Contains (Authorizer authorizer) {
            return list.Contains(authorizer);
        }
        
        internal Authorizer ToAuthorizer() {
            if (list == null)
                return AuthorizeDeny.Instance;
            if (list.Count == 0)
                return AuthorizeDeny.Instance;
            return TrimAny(list);
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