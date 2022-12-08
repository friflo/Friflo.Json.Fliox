// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Hub.Host
{
    // ------------------------------------ SyncContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="FlioxHub.ExecuteRequestAsync"/> call to enable
    /// multi threaded / concurrent handling of a <see cref="SyncRequest"/>.
    /// </summary>
    /// <remarks>
    /// Note: In case of adding transaction support for <see cref="SyncRequest"/>'s in future transaction data / state
    /// need to be handled by this class.
    /// </remarks>
    public sealed class SyncContext
    {
        // --- public
        public              FlioxHub                    Hub             => hub;
        public              JsonKey                     ClientId        => clientId;
        public              User                        User            => authState.user;
        public              bool                        Authenticated   => authState.authenticated;
        public              SmallString                 DatabaseName    => databaseName;                    // not null
        public              EntityDatabase              Database        => hub.GetDatabase(databaseName);   // not null
        public              ObjectPool<ObjectMapper>    ObjectMapper    => pool.ObjectMapper;
        public              ObjectPool<EntityProcessor> EntityProcessor => pool.EntityProcessor;
        public              MemoryBuffer                MemoryBuffer    => memoryBuffer;
        public              SyncRequest                 Request         => request;

        // --- internal / private by intention
        /// <summary>
        /// Note!  Keep <see cref="pool"/> internal
        /// Its <see cref="ObjectPool{T}"/> instances can be made public as properties if required
        /// </summary>
        internal  readonly  Pool                    pool;
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        internal  readonly  EventReceiver           eventReceiver;
        internal  readonly  SharedCache             sharedCache;
        internal  readonly  SyncBuffers             syncBuffers;
        internal            AuthState               authState;
        internal            Action                  canceler = () => {};
        internal            FlioxHub                hub;
        internal            JsonKey                 clientId;
        internal            SmallString             databaseName;              // not null
        internal            ClientIdValidation      clientIdValidation;
        internal            SyncRequest             request;
        private             MemoryBuffer            memoryBuffer;

        public override     string                  ToString() => GetString();
        
        public void Init () {
            authState           = default;
            canceler            = null;
            hub                 = null;
            clientId            = default;
            databaseName        = default;
            clientIdValidation  = default;
            request             = null;
            memoryBuffer        = null;
        }

        public SyncContext (SharedEnv sharedEnv, EventReceiver eventReceiver, MemoryBuffer memoryBuffer) {
            this.pool           = sharedEnv.Pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.memoryBuffer   = memoryBuffer ?? throw new ArgumentNullException(nameof(memoryBuffer));
            memoryBuffer.Reset();
        }
        
        /// <summary>Special constructor used to minimize heap allocation. <b>Note</b> <see cref="SyncBuffers"/> </summary>
        public SyncContext (SharedEnv sharedEnv, EventReceiver eventReceiver, in SyncBuffers syncBuffers) {
            this.pool           = sharedEnv.Pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.syncBuffers    = syncBuffers;
        }
        
        public SyncContext (SharedEnv sharedEnv, EventReceiver eventReceiver) {
            this.pool           = sharedEnv.Pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
        }
        
        public void SetMemoryBuffer (MemoryBuffer memoryBuffer) {
            this.memoryBuffer   = memoryBuffer ?? throw new ArgumentNullException(nameof(memoryBuffer));
            memoryBuffer.Reset();
        }

        public void AuthenticationFailed(User user, string error, TaskAuthorizer taskAuthorizer, HubPermission hubPermission) {
            AssertAuthenticationParams(user, taskAuthorizer, hubPermission);
            authState.user              = user;
            authState.authExecuted      = true;
            authState.authenticated     = false;
            authState.taskAuthorizer    = taskAuthorizer;
            authState.hubPermission     = hubPermission;
            authState.error             = error;
        }
        
        public void AuthenticationSucceed (User user, TaskAuthorizer taskAuthorizer, HubPermission hubPermission) {
            AssertAuthenticationParams(user, taskAuthorizer, hubPermission);
            authState.user              = user;
            authState.authExecuted      = true;
            authState.authenticated     = true;
            authState.taskAuthorizer    = taskAuthorizer;
            authState.hubPermission     = hubPermission;
        }
        
        public void SetClientId(in JsonKey clientId) {
            this.clientId       = clientId;
            clientIdValidation  = ClientIdValidation.Valid;
        }
        
        [Conditional("DEBUG")]
        private void AssertAuthenticationParams(User user, TaskAuthorizer taskAuthorizer, HubPermission hubPermission) {
            if (authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (user == null)           throw new ArgumentNullException(nameof(user));
            if (taskAuthorizer == null) throw new ArgumentNullException(nameof(taskAuthorizer));
            if (hubPermission == null)  throw new ArgumentNullException(nameof(hubPermission));
        }
        
        private string GetString() {
            // extracted method to avoid boxing authState struct in string interpolation which causes noise in Rider > Debug > Memory list
            var user    = authState.user;
            var state   = authState.ToString();
            return $"userId: {user}, auth: {state}";
        }
        
        internal void Cancel() {
            canceler(); // canceler.Invoke();
        }
    }
    
    /// <summary>
    /// <see cref="SyncBuffers"/> can be used to minimize heap allocations by passing to <see cref="SyncContext"/> constructor. <br/>
    /// <b>Note</b> The the caller of <see cref="FlioxHub.ExecuteRequestAsync"/> <b>must</b> ensure that only one call to
    /// <see cref="FlioxHub.ExecuteRequestAsync"/> is running at a time.<br/>
    /// This requirement is fulfilled if request execution is stream based like <see cref="Remote.WebSocketHost"/>
    /// </summary>
    public readonly struct SyncBuffers
    {
        internal readonly List<SyncRequestTask> eventTasks;
        
        public SyncBuffers (List<SyncRequestTask> eventTasks) {
            this.eventTasks   = eventTasks;
        }
    }
    
    /// <summary>
    /// Contains the result of a <see cref="FlioxHub.ExecuteRequestAsync"/> call. <br/>
    /// After execution either <see cref="success"/> or <see cref="error"/> is set. Never both.
    /// </summary>
    public readonly struct ExecuteSyncResult {
        public   readonly   SyncResponse    success;
        public   readonly   ErrorResponse   error;

        public ExecuteSyncResult (SyncResponse successResponse) {
            success = successResponse ?? throw new ArgumentNullException(nameof(successResponse));
            error   = null;
        }
        
        public ExecuteSyncResult (string errorMessage, ErrorResponseType errorType) {
            success = null;
            error   = new ErrorResponse { message = errorMessage, type = errorType };
        }
        
        public  ProtocolResponse Result { get {
            if (success != null)
                return success;
            return error;
        } }

        public override string ToString() => success != null ? success.ToString() : error.ToString();
    }
}
