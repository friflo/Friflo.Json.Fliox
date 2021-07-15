// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

#if UNITY_5_3_OR_NEWER
    using ValueTask = System.Threading.Tasks.Task;
#endif

namespace Friflo.Json.Flow.Database.Auth
{
    /// <summary>
    /// Performs authentication and authorization by checking <see cref="SyncRequest.clientId"/> and <see cref="SyncRequest.token"/>
    /// in every <see cref="EntityDatabase.ExecuteSync"/> call.
    /// </summary>
    public abstract class Authenticator
    {
        protected readonly Dictionary<string, AuthorizePredicate> registeredPredicates = new Dictionary<string, AuthorizePredicate>();
            
        public abstract ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext);
        
        public void RegisterPredicate(string name, AuthPredicate predicate) {
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        } 
    }
    
    public class AuthenticateNone : Authenticator
    {
        private readonly Authorizer unknown;

        public AuthenticateNone(Authorizer unknown) {
            this.unknown = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
#pragma warning disable 1998   // This async method lacks 'await' operators and will run synchronously. ....
        public override async ValueTask Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.authState.SetFailed("not authenticated", unknown);
        }
    }
}