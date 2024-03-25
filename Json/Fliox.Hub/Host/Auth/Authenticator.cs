// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Protocol;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    /// <summary>
    /// Performs authentication and authorization by checking <see cref="SyncRequest.userId"/> and <see cref="SyncRequest.token"/>
    /// in every <see cref="FlioxHub.ExecuteRequestAsync"/> call.
    /// </summary>
    /// <remarks>
    /// <see cref="Authenticator"/> is mutable. Its <see cref="users"/> and <see cref="registeredPredicates"/> are subject to change.
    /// </remarks>
    public abstract class Authenticator
    {
        protected readonly  Dictionary<string, AuthorizePredicate>  registeredPredicates;
        [DebuggerBrowsable(Never)]
        internal  readonly  ConcurrentDictionary<ShortString, User> users;  // todo make private
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<User>                       Users => users.Values;
        internal            HashSet<string>                         GetRegisteredPredicates() => registeredPredicates.Keys.ToHashSet();
        
        public    override  string                                  ToString() => $"users: {users.Count}";

        public abstract Task    AuthenticateAsync   (SyncRequest syncRequest, SyncContext syncContext);
        public virtual  void    Authenticate        (SyncRequest syncRequest, SyncContext syncContext)
            => throw new NotSupportedException("Authenticator supports only asynchronous authentication");
        public virtual  bool    IsSynchronous       (SyncRequest syncRequest) => false;
        
        protected Authenticator () {
            registeredPredicates    = new Dictionary<string, AuthorizePredicate>();
            users                   = new ConcurrentDictionary <ShortString, User>(ShortString.Equality);
        }
        
        /// <summary>
        /// Validate <see cref="SyncContext.clientId"/> and returns <see cref="ClientIdValidation"/> result.
        /// </summary>
        public virtual ClientIdValidation ValidateClientId(ClientController clientController, SyncContext syncContext) {
            ref var clientId = ref syncContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            var user = syncContext.User;
            if (clientController.UseClientIdFor(user, clientId))
                return ClientIdValidation.Valid;
            return ClientIdValidation.Invalid;
        }
        
        public virtual bool EnsureValidClientId(ClientController clientController, SyncContext syncContext, out string error) {
            switch (syncContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.IsNull:
                    error           = null;
                    var user        = syncContext.User;
                    var clientId    = clientController.NewClientIdFor(user);
                    syncContext.SetClientId(clientId);
                    return true;
                case ClientIdValidation.Invalid:
                    error = "invalid clientId";
                    return false;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }
        
        public virtual Task SetUserOptionsAsync (User user, UserParam param) {
            user.SetUserOptions(param);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register a predicate function by the given <paramref name="name"/> which enables custom authorization via code,
        /// which cannot be expressed by one of the provided <see cref="TaskRight"/> implementations.
        /// If called its parameters are intended to filter the aspired condition and return true if task execution is granted.
        /// To reject task execution it returns false.
        /// </summary>
        public void RegisterPredicate(string name, AuthPredicate predicate) {
            var authorizer = new AuthorizePredicate (name, predicate);
            registeredPredicates.Add(name, authorizer);
        }
        
        /// <summary>
        /// Register a predicate function which enables custom authorization via code, which cannot be expressed by one of the
        /// provided <see cref="TaskRight"/> implementations.
        /// The <paramref name="predicate"/> is registered by its delegate name.
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
                var requestCounts = pair.Value.requestCounts;
                lock (requestCounts) {
                    requestCounts.Clear();
                }
            }
        }
    }
    
    /// <summary>
    /// Represent the result of client id validation returned by <see cref="Authenticator.ValidateClientId"/>  
    /// </summary>
    public enum ClientIdValidation {
        IsNull  = 1,
        Invalid = 2,
        Valid   = 3
    }
}