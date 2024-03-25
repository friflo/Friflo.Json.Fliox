// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
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
    public sealed partial class SyncContext
    {
        // --- public
                        public  FlioxHub        Hub             => hub;
                        public  ShortString     ClientId        => clientId;
                        public  User            User            => authState.user;
                        public  bool            Authenticated   => authState.authenticated;
                        public  EntityDatabase  Database        => database;                // not null
                        public  MemoryBuffer    MemoryBuffer    => memoryBuffer;
                        public  SyncRequest     Request         => request;
                        public  IHost           Host            { get => host; init => host = value ?? throw new ArgumentNullException(nameof(Host)); }
                        
        [Browse(Never)] public  ObjectPool<Json2SQL>    Json2SQL    => pool.Json2SQL;
        [Browse(Never)] public  ObjectPool<SQL2Json>    SQL2Json    => pool.SQL2Json;
        // --- public utils
        [Browse(Never)] public  ObjectPool<ObjectMapper>    ObjectMapper                => pool.ObjectMapper;
        [Browse(Never)] public  ObjectPool<EntityProcessor> EntityProcessor             => pool.EntityProcessor;
                        public  TypeMapper                  GetTypeMapper(Type type)    => sharedEnv.typeStore.GetTypeMapper(type);
        public override         string                      ToString()                  => GetString();
        // --- internal / private by intention
        [Browse(Never)] internal  readonly  SharedEnv           sharedEnv;
        /// <summary>
        /// Note!  Keep <see cref="pool"/> internal
        /// Its <see cref="ObjectPool{T}"/> instances can be made public as properties if required
        /// </summary>
        [Browse(Never)] internal  readonly  Pool                pool;
        /// <summary>Is set for clients requests only. In other words - from the initiator of a <see cref="ProtocolRequest"/></summary>
        [Browse(Never)] internal  readonly  IEventReceiver      eventReceiver;
        [Browse(Never)] internal  readonly  SharedCache         sharedCache;
        [Browse(Never)] internal  readonly  SyncBuffers         syncBuffers;
        [Browse(Never)] internal  readonly  SyncPools           syncPools;
        [Browse(Never)] private   readonly  IHost               host;
        [Browse(Never)] internal            AuthState           authState;
        [Browse(Never)] internal            Action              canceler = () => {};
        [Browse(Never)] internal            FlioxHub            hub;
        [Browse(Never)] internal            ShortString         clientId;
        [Browse(Never)] internal            EntityDatabase      database;           // not null
        [Browse(Never)] internal            ClientIdValidation  clientIdValidation;
        [Browse(Never)] internal            SyncRequest         request;
        [Browse(Never)] internal            SyncResponse        response;
        [Browse(Never)] private             MemoryBuffer        memoryBuffer;
        [Browse(Never)] internal            ReaderPool          responseReaderPool;
        
        public void Init () {
            authState           = default;
            canceler            = null;
            hub                 = null;
            clientId            = default;
            database            = null;
            clientIdValidation  = default;
            request             = null;
            response            = null;
            memoryBuffer        = null;
            responseReaderPool  = null;
        }

        public SyncContext (SharedEnv sharedEnv, IEventReceiver eventReceiver, MemoryBuffer memoryBuffer) {
            this.sharedEnv      = sharedEnv;
            this.pool           = sharedEnv.pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.memoryBuffer   = memoryBuffer ?? throw new ArgumentNullException(nameof(memoryBuffer));
            memoryBuffer.Reset();
        }
        
        /// <summary>Special constructor used to minimize heap allocation. <b>Note</b> <see cref="SyncBuffers"/> </summary>
        public SyncContext (SharedEnv sharedEnv, IEventReceiver eventReceiver, in SyncBuffers syncBuffers, SyncPools syncPools) {
            this.sharedEnv      = sharedEnv;
            this.pool           = sharedEnv.pool;
            this.eventReceiver  = eventReceiver;
            this.sharedCache    = sharedEnv.sharedCache;
            this.syncBuffers    = syncBuffers;
            this.syncPools      = syncPools;
        }
        
        public SyncContext (SharedEnv sharedEnv, IEventReceiver eventReceiver) {
            this.sharedEnv      = sharedEnv;
            this.pool           = sharedEnv.pool;
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
        
        public void SetClientId(in ShortString clientId) {
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
        
        private     ISyncConnection     connection;
        private     SyncTransaction     transaction;
        internal    bool                IsTransactionPending => transaction != null;
        
        public async Task<ISyncConnection> GetConnectionAsync () {
            if (connection != null) {
                return connection;
            }
            return connection = await Database.GetConnectionAsync().ConfigureAwait(false);
        }
        
        public ISyncConnection GetConnectionSync () {
            if (connection != null) {
                return connection;
            }
            return connection = Database.GetConnectionSync();
        }
        
        internal void ReturnConnection() {
            if (connection != null && connection.IsOpen) {
                Database.ReturnConnection(connection);
                connection = null;
            }
        }
        
        internal async Task<TransResult> TransactionAsync(TransCommand command, int taskIndex) {
            var db      = Database;
            var trans   = transaction;
            switch (command)
            {
                case TransCommand.Begin: {
                    if (trans != null) {
                        return new TransResult("Transaction already started");
                    }
                    transaction = new SyncTransaction(taskIndex);
                    return await db.TransactionAsync(this, TransCommand.Begin).ConfigureAwait(false);
                }
                case TransCommand.Commit: {
                    if (trans == null) {
                        return new TransResult("Missing begin transaction");
                    }
                    transaction = null;
                    bool success = !SyncTransaction.HasTaskErrors(response.tasks, trans.beginTask + 1, taskIndex);
                    TransResult result;
                    if (success) {
                        result = await db.TransactionAsync(this, TransCommand.Commit).ConfigureAwait(false);
                        if (result.error != null) {
                            result = await db.TransactionAsync(this, TransCommand.Rollback).ConfigureAwait(false);
                        }
                    } else {
                        result = await db.TransactionAsync(this, TransCommand.Rollback).ConfigureAwait(false);
                    }
                    UpdateTransactionBeginResult(trans.beginTask, result);
                    return result;
                }
                case TransCommand.Rollback: {
                    if (trans == null) {
                        return new TransResult("Missing begin transaction");
                    }
                    transaction = null;
                    var result = await db.TransactionAsync(this, TransCommand.Rollback).ConfigureAwait(false);
                    UpdateTransactionBeginResult(trans.beginTask, result);
                    return result;
                }
            }
            return new TransResult($"invalid transaction command: {command}");
        }
        
        private void UpdateTransactionBeginResult(int beginTask, TransResult transResult) {
            if (transResult.error != null) {
                response.tasks[beginTask] = new TaskErrorResult(TaskErrorType.CommandError, transResult.error);
                return;
            }
            if (transResult.state == TransCommand.Commit) {
                using (var pooled = pool.ObjectMapper.Get()) {
                    var writer      = pooled.instance.writer;
                    var result      = SyncTransaction.CreateResult(TransCommand.Commit);
                    var jsonResult  = writer.WriteAsValue(result);
                    response.tasks[beginTask] = new SendCommandResult { result = jsonResult };
                }
            }
        }
    }
    
    public interface IHost { }
    
    public interface IHttpHost : IHost {
        List<string>    Routes { get; }
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
        internal readonly InstancePool<ListOne<SyncTaskResult>> taskResultsPool;
        internal readonly InstancePool<SyncResponse>            responsePool;
        internal readonly InstancePool<UpsertEntitiesResult>    upsertResultPool;
        internal readonly InstancePool<SendMessageResult>       messageResultPool;
        
        public SyncPools() {
            pools               = new InstancePools();
            taskResultsPool     = new InstancePool<ListOne<SyncTaskResult>>(pools);
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
    

}
