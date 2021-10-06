// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    /// <summary>
    /// Performs authentication and authorization by checking <see cref="SyncRequest.userId"/> and <see cref="SyncRequest.token"/>
    /// in every <see cref="EntityDatabase.ExecuteSync"/> call.
    /// </summary>
    public abstract class Authenticator
    {
        protected readonly Dictionary<string, AuthorizePredicate> registeredPredicates = new Dictionary<string, AuthorizePredicate>();
            
        public abstract Task    Authenticate    (SyncRequest syncRequest, MessageContext messageContext);
        
        public virtual JsonKey ValidateClientId(SyncRequest syncRequest) {
            return syncRequest.clientId;
        }
        
        /// <summary>
        /// Used by tasks which require a client id. E.g. <see cref="SubscribeMessage"/> or <see cref="SubscribeChanges"/> 
        /// In case <see cref="MessageContext.clientId"/> is null a new one is created and assigned
        /// </summary>
        public virtual void EnsureValidClientId(IClientIdProvider clientIdProvider, MessageContext messageContext) {
            if (!messageContext.clientId.IsNull())
                return;
            messageContext.clientId = clientIdProvider.NewClientId();
        }

        /// <summary>
        /// Register a predicate function by the given <see cref="name"/> which enables custom authorization via code,
        /// which cannot be expressed by one of the provided <see cref="Right"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(string name, AuthPredicate predicate) {
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
        
        /// <summary>
        /// Register a predicate function which enables custom authorization via code, which cannot be expressed by one of the
        /// provided <see cref="Right"/> implementations.
        /// The <see cref="predicate"/> is registered by its delegate name.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(AuthPredicate predicate) {
            var name = predicate.Method.Name;
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
    }
    
    public sealed class AuthenticateNone : Authenticator
    {
        private readonly Authorizer unknown;

        public AuthenticateNone(Authorizer unknown) {
            this.unknown = unknown ?? throw new NullReferenceException(nameof(unknown));
        }
        
        public override Task Authenticate(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.authState.SetFailed("not authenticated", unknown);
            return Task.CompletedTask;
        }
    }
}