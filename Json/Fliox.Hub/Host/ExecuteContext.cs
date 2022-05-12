// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    // ------------------------------------ ExecuteContext ------------------------------------
    /// <summary>
    /// One <see cref="ExecuteContext"/> is created per <see cref="ProtocolMessage"/> to enable
    /// multi threaded / concurrent request, response and event handling (processing).
    /// These message types are represented by <see cref="ProtocolRequest"/>, <see cref="ProtocolResponse"/> and
    /// <see cref="ProtocolEvent"/>.
    /// <br></br>
    /// Note: In case of adding transaction support for <see cref="SyncRequest"/>'s in future transaction data / state
    /// need to be handled by this class.
    /// </summary>
    public sealed class ExecuteContext
    {
        // --- public
        public              FlioxHub                    Hub             => hub;
        public              JsonKey                     clientId;
        public              ClientIdValidation          clientIdValidation;
        public              User                        User            => authState.user;
        public              bool                        Authenticated   => authState.authenticated;
        public              string                      DatabaseName    { get; internal set; }              // not null
        public              EntityDatabase              Database        => hub.GetDatabase(DatabaseName);   // not null
        public              ObjectPool<ObjectMapper>    ObjectMapper    => pool.ObjectMapper;
        public              ObjectPool<EntityProcessor> EntityProcessor => pool.EntityProcessor;

        // --- internal / private by intention
        /// <summary>
        /// Note!  Keep <see cref="pool"/> internal
        /// Its <see cref="ObjectPool{T}"/> instances can be made public as properties if required
        /// </summary>
        internal  readonly  IPool               pool;
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        internal  readonly  IEventTarget        eventTarget;
        internal            AuthState           authState;
        private             PoolUsage           startUsage;
        internal            Action              canceler = () => {};
        internal            FlioxHub            hub;
        internal  readonly  SharedCache         sharedCache;
        
        public override     string              ToString() => $"userId: {authState.user}, auth: {authState}";

        internal ExecuteContext (IPool pool, IEventTarget eventTarget, SharedCache sharedCache) {
            this.pool           = pool;
            startUsage          = pool.PoolUsage;
            this.eventTarget    = eventTarget;
            this.sharedCache    = sharedCache;
        }
        
        internal ExecuteContext (IPool pool, IEventTarget eventTarget, SharedCache sharedCache, in JsonKey clientId) {
            this.pool           = pool;
            startUsage          = pool.PoolUsage;
            this.eventTarget    = eventTarget;
            this.clientId       = clientId;
            this.sharedCache    = sharedCache;
        }
        
        public void AuthenticationFailed(User user, string error, Authorizer authorizer) {
            AssertAuthenticationParams(user, authorizer);
            authState.user            = user;
            authState.authExecuted    = true;
            authState.authenticated   = false;
            authState.authorizer      = authorizer;
            authState.error           = error;
        }
        
        public void AuthenticationSucceed (User user, Authorizer authorizer) {
            AssertAuthenticationParams(user, authorizer);
            authState.user            = user;
            authState.authExecuted    = true;
            authState.authenticated   = true;
            authState.authorizer      = authorizer;
        }
        
        [Conditional("DEBUG")]
        private void AssertAuthenticationParams(User user, Authorizer authorizer) {
            if (authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (user == null)           throw new ArgumentNullException(nameof(user));
            if (authorizer == null)     throw new ArgumentNullException(nameof(authorizer));
        }
        
        internal void Cancel() {
            canceler(); // canceler.Invoke();
        }

        internal void Release() {
            startUsage.AssertEqual(pool.PoolUsage);
        }
    }
}
