// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        protected readonly  Dictionary<string, AuthorizePredicate>  registeredPredicates;
        internal  readonly  ConcurrentDictionary<JsonKey, User>     users;  // todo make private
        protected readonly  User                                    anonymousUser;
        
            
        public abstract Task    Authenticate    (SyncRequest syncRequest, MessageContext messageContext);
        
        protected Authenticator (Authorizer anonymousAuthorizer) {
            registeredPredicates    = new Dictionary<string, AuthorizePredicate>();
            users                   = new ConcurrentDictionary <JsonKey, User>(JsonKey.Equality);
            anonymousUser           = new User(User.AnonymousId, null, anonymousAuthorizer); 
            users.TryAdd(User.AnonymousId, anonymousUser);
        }
        
        /// <summary>
        /// Validate <see cref="MessageContext.clientId"/> and returns <see cref="ClientIdValidation"/> result.
        /// </summary>
        public virtual ClientIdValidation ValidateClientId(ClientController clientController, MessageContext messageContext) {
            ref var clientId = ref messageContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            var user = messageContext.authState.User;
            if (clientController.UseClientIdFor(user, clientId))
                return ClientIdValidation.Valid;
            return ClientIdValidation.Invalid;
        }
        
        public virtual bool EnsureValidClientId(ClientController clientController, MessageContext messageContext, out string error) {
            switch (messageContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.IsNull:
                    error = null;
                    var user                            = messageContext.authState.User;
                    messageContext.clientId             = clientController.NewClientIdFor(user);
                    messageContext.clientIdValidation   = ClientIdValidation.Valid;
                    return true;
                case ClientIdValidation.Invalid:
                    error = "invalid clientId";
                    return false;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
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
        
        internal void ClearUserStats() {
            foreach (var pair in users) {
                pair.Value.requestCounts.Clear();
            }
        }
    }
    
    /// <summary>
    /// Represent the result of client id validation returned by <see cref="Authenticator.ValidateClientId"/>  
    /// </summary>
    public enum ClientIdValidation {
        IsNull,
        Invalid,
        Valid
    }
}