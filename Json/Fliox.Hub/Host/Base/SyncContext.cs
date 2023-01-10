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
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
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

        public override     string                      ToString()      => GetString();
        // --- internal / private by intention
        /// <summary>
        /// Note!  Keep <see cref="pool"/> internal
        /// Its <see cref="ObjectPool{T}"/> instances can be made public as properties if required
        /// </summary>
        [Browse(Never)] internal  readonly  Pool                pool;
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        [Browse(Never)] internal  readonly  EventReceiver       eventReceiver;
        [Browse(Never)] internal  readonly  SharedCache         sharedCache;
        [Browse(Never)] internal  readonly  SyncBuffers         syncBuffers;
        [Browse(Never)] internal  readonly  SyncPools           syncPools;
        [Browse(Never)] internal            AuthState           authState;
        [Browse(Never)] internal            Action              canceler = () => {};
        [Browse(Never)] internal            FlioxHub            hub;
        [Browse(Never)] internal            JsonKey             clientId;
        [Browse(Never)] internal            SmallString         databaseName;              // not null
        [Browse(Never)] internal            ClientIdValidation  clientIdValidation;
        [Browse(Never)] internal            SyncRequest         request;
        [Browse(Never)] internal            ReaderInstancePool  responseInstancePool;   
        [Browse(Never)] private             MemoryBuffer        memoryBuffer;
        
        public void Init () {
            authState               = default;
            canceler                = null;
            hub                     = null;
            clientId                = default;
            databaseName            = default;
            clientIdValidation      = default;
            request                 = null;
            memoryBuffer            = null;
            responseInstancePool    = null;
        }

        public SyncContext (SharedEnv sharedEnv, EventReceiver eventReceiver, MemoryBuffer memoryBuffer) {
            this.pool           = sharedEnv.Pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.memoryBuffer   = memoryBuffer ?? throw new ArgumentNullException(nameof(memoryBuffer));
            memoryBuffer.Reset();
        }
        
        /// <summary>Special constructor used to minimize heap allocation. <b>Note</b> <see cref="SyncBuffers"/> </summary>
        public SyncContext (SharedEnv sharedEnv, EventReceiver eventReceiver, in SyncBuffers syncBuffers, SyncPools syncPools) {
            this.pool           = sharedEnv.Pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.syncBuffers    = syncBuffers;
            this.syncPools      = syncPools;
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
        internal readonly List<SyncRequestTask> syncTasks;
        internal readonly List<SyncRequestTask> eventTasks;
        internal readonly List<JsonValue>       tasksJson;
        
        public SyncBuffers (List<SyncRequestTask> syncTasks, List<SyncRequestTask> eventTasks, List<JsonValue> tasksJson) {
            this.syncTasks  = syncTasks;
            this.eventTasks = eventTasks;
            this.tasksJson  = tasksJson;
        }
    }
    
    public sealed class SyncPools
    {
        private  readonly InstancePools                         pools;
        internal readonly InstancePool<List<SyncTaskResult>>    taskResultsPool;
        internal readonly InstancePool<SyncResponse>            responsePool;
        internal readonly InstancePool<UpsertEntitiesResult>    upsertResultPool;
        internal readonly InstancePool<SendMessageResult>       messageResultPool;
        
        public SyncPools(TypeStore typeStore) {
            pools               = new InstancePools(typeStore);
            taskResultsPool     = new InstancePool<List<SyncTaskResult>>(pools);
            responsePool        = new InstancePool<SyncResponse>        (pools);
            upsertResultPool    = new InstancePool<UpsertEntitiesResult>(pools);
            messageResultPool   = new InstancePool<SendMessageResult>   (pools);
        }
        
        public void Reuse() {
            pools.Reuse();
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
    
    /// <summary> Define the how to execute a <see cref="SyncRequest"/> </summary>
    /// <remarks>
    /// It is used to enable:<br/>
    /// 1. Execute a <see cref="SyncRequest"/> with a synchronous call if possible to avoid heap allocation
    ///    and CPU costs required for asynchronous methods if possible<br/>
    /// 2. Enable queued execution of <see cref="SyncRequest"/>. See <see cref="DatabaseService.ExecuteQueuedRequestsAsync"/><br/>
    /// </remarks>
    public enum ExecutionType {
        None    = 0,
        /// <summary>execute request error synchronous with <see cref="FlioxHub.ExecuteRequest"/></summary>
        Error   = 1,
        /// <summary>execute request synchronous with <see cref="FlioxHub.ExecuteRequest"/></summary>
        Sync    = 2,
        /// <summary>execute request asynchronous with <see cref="FlioxHub.ExecuteRequestAsync"/></summary>
        Async   = 3,
        /// <summary>queue request execution with <see cref="FlioxHub.QueueRequestAsync"/></summary>
        Queue   = 4,
    }
}
