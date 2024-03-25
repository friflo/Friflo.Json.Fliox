// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
// using System.Threading.Tasks; intentionally not used in sync version
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note!  Keep file in sync with:  FlioxHub-async.cs

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public partial class FlioxHub
    {
        /// <summary>
        /// Execute all <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/> send by client.
        /// </summary>
        /// <remarks>
        /// All requests to a <see cref="FlioxHub"/> are handled by this method.
        /// By design this is the 'front door' all requests have to pass to get processed.
        /// <para>
        ///   <see cref="ExecuteRequestAsync"/> catches exceptions thrown by a <see cref="SyncRequestTask"/> but 
        ///   this is only a fail safe mechanism.
        ///   Thrown exceptions need to be handled by proper error handling in the first place.
        ///
        ///   Reasons for the design decision: 
        ///   <para> a) Without a proper error handling the root cause of an error cannot be traced back.</para>
        ///   <para> b) Exceptions are expensive regarding CPU utilization and heap allocation.</para>
        /// </para>
        /// <para>
        ///   An exception can have two different reasons:
        ///   <para> 1. The implementation of an <see cref="EntityContainer"/> is missing a proper error handling.
        ///          A proper error handling requires to set a meaningful <see cref="Protocol.Models.TaskExecuteError"/> to
        ///          <see cref="Protocol.Models.ITaskResultError.Error"/></para>
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.Hub.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </remarks>
        public ExecuteSyncResult ExecuteRequest(SyncRequest syncRequest, SyncContext syncContext)
        {
            switch (syncRequest.intern.executionType) {
                case Error: return new ExecuteSyncResult(syncRequest.intern.error, ErrorResponseType.BadRequest);
                case Sync:  break;
                default:    return new ExecuteSyncResult($"Invalid execution type: {syncRequest.intern.executionType}", ErrorResponseType.Exception);
            }
            syncContext.request             = syncRequest;
            if (syncContext.authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (authenticator.IsSynchronous(syncRequest)) {
                authenticator.Authenticate(syncRequest, syncContext);
            } else {
                throw new NotSupportedException("Authenticator supports only asynchronous authentication");
            }
            if (syncRequest.intern.error != null) {
                return new ExecuteSyncResult (syncRequest.intern.error, ErrorResponseType.BadRequest); 
            }
            syncContext.hub                 = this;
            var db = syncContext.database   = syncRequest.intern.db;
            syncContext.clientId            = syncRequest.clientId;
            syncContext.clientIdValidation  = authenticator.ValidateClientId(clientController, syncContext);
            
            var service         = db.service;
            var requestTasks    = syncRequest.tasks;
            var taskCount       = requestTasks.Count;

            service.PreExecuteTasks(syncContext);
            
            syncContext.syncPools?.Reuse();

            var response        = SyncResponse.Create(syncContext, taskCount);
            syncContext.response= response;
            response.database   = syncRequest.database;

            // ------------------------ loop through all given tasks and execute them ------------------------
            for (int index = 0; index < taskCount; index++) {
                var task = requestTasks[index];
                try {
                    // Execute task synchronous.
                    SyncTaskResult result = service.ExecuteTask(task, db, response, syncContext);
                    //
                    // Ensuring that task can be executed synchronously is a result
                    // from preceding InitSyncRequest() call.
                    //
                    //
                    response.tasks.Add(result);
                } catch (Exception e) {
                    response.tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                    var message = GetLogMessage(db.name, syncRequest.userId, index, task);
                    Logger.Log(HubLog.Error, message, e);
                }
            }
            if (syncContext.IsTransactionPending) {
                syncContext.Transaction(TransCommand.Commit, taskCount);
            }
            syncContext.ReturnConnection();
            PostExecute(syncRequest, response, syncContext);
            return new ExecuteSyncResult(response);
        }
    }
}
