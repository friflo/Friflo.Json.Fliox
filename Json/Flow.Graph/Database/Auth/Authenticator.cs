// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.Database.Auth
{
    public abstract class Authenticator
    {
        public abstract ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext);
    }
    
    public class AuthenticateNone : Authenticator
    {
        private readonly Authorizer anonymousAuthorizer;

        public AuthenticateNone(Authorizer anonymousAuthorizer) {
            this.anonymousAuthorizer = anonymousAuthorizer ?? throw new NullReferenceException(nameof(anonymousAuthorizer));
        }
        
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
    
    public class AuthorizeReadOnly : Authorizer {
        public override bool Authorize(DatabaseTask task, MessageContext messageContext) {
            switch (task.TaskType) {
                case TaskType.query:
                case TaskType.read:
                case TaskType.message:
                case TaskType.subscribeChanges:
                case TaskType.subscribeMessage:
                    return true;
            }
            return false;
        }
    }
}