// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.Database.Auth
{
    public abstract class Authenticator
    {
        public readonly Authorizer anonymousAuthorizer;

        protected Authenticator (Authorizer anonymousAuthorizer = null) {
            this.anonymousAuthorizer = anonymousAuthorizer ?? new AuthorizeAll();
        }
        
        public abstract ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext);
    }
    
    public class DefaultAuthenticator : Authenticator
    {
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.authState.SetFailed("not authenticated", anonymousAuthorizer);
        }
    }
    
    public abstract class Authorizer
    {
        public abstract bool Authorize(DatabaseTask task, MessageContext messageContext);
    }
    
    public class AuthorizeAll : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return true;
        }
    }
    
    public class AuthorizeNone : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            return false;
        }
    }
}