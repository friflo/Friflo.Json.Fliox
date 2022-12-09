// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// Note!  Keep file in sync with:  FlioxHub.execute.sync.cs

// ReSharper disable MethodHasAsyncOverload
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
        ///          A proper error handling requires to set a meaningful <see cref="Protocol.Models.CommandError"/> to
        ///          <see cref="Protocol.Models.ICommandResult.Error"/></para>
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.Hub.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </remarks>
        public virtual async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            syncContext.request             = syncRequest;
            if (syncContext.authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (authenticator.IsSynchronous) {
                authenticator.Authenticate(syncRequest, syncContext);
            } else {
                await authenticator.AuthenticateAsync(syncRequest, syncContext).ConfigureAwait(false);
            }
            syncContext.hub = this;
            var syncDbName  = new SmallString(syncRequest.database);        // is nullable
            var hubDbName   = syncContext.hub.DatabaseName;                 // not null
            var dbName      = syncDbName.IsNull() ? hubDbName : syncDbName; // not null
            syncContext.databaseName        = dbName;
            syncContext.clientId            = syncRequest.clientId;
            syncContext.clientIdValidation  = authenticator.ValidateClientId(clientController, syncContext);
            
            // todo check extracting validation to ValidateTasks()
            var requestTasks = syncRequest.tasks;
            if (requestTasks == null) {
                return new ExecuteSyncResult ("missing field: tasks (array)", ErrorResponseType.BadRequest);
            }
            var taskCount = requestTasks.Count;
            EntityDatabase db = database;
            if (!dbName.IsEqual(hubDbName)) {
                if (!extensionDbs.TryGetValue(dbName, out db))
                    return new ExecuteSyncResult($"database not found: '{syncRequest.database}'", ErrorResponseType.BadRequest);
            }
            for (int index = 0; index < taskCount; index++) {
                var task = requestTasks[index];
                if (task != null) {
                    task.index          = index;
                    task.isSynchronous  = db.PreExecute(task);
                    continue;
                }
                return new ExecuteSyncResult($"tasks[{index}] == null", ErrorResponseType.BadRequest);
            }
            var   service = db.service;
            service.PreExecuteTasks(syncContext);

            var tasks       = new List<SyncTaskResult>(taskCount);
            var response    = new SyncResponse { tasks = tasks, database = syncDbName.value };
            
            // ------------------------ loop through all given tasks and execute them ------------------------
            for (int index = 0; index < taskCount; index++) {
                var task = requestTasks[index];
                try {
                    // Execute task synchronous or asynchronous.
                    SyncTaskResult result;
                    if (task.isSynchronous) {
                        result = service.ExecuteTask(task, db, response, syncContext);
                    } else {
                        result = await service.ExecuteTaskAsync(task, db, response, syncContext).ConfigureAwait(false);
                    }
                    tasks.Add(result);
                } catch (Exception e) {
                    tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                    var message = GetLogMessage(dbName.value, syncRequest.userId, index, task);
                    Logger.Log(HubLog.Error, message, e);
                }
            }
            hostStats.Update(syncRequest);
            UpdateRequestStats(dbName, syncRequest, syncContext);

            // - Note: Only relevant for Push messages when using a bidirectional protocol like WebSocket
            // As a client is required to use response.clientId it is set to null if given clientId was invalid.
            // So next request will create a new valid client id.
            response.clientId = syncContext.clientIdValidation == ClientIdValidation.Invalid ? new JsonKey() : syncContext.clientId;
            
            response.AssertResponse(syncRequest);
            
            service.PostExecuteTasks(syncContext);
            
            var dispatcher = EventDispatcher;
            if (dispatcher != null) {
                dispatcher.EnqueueSyncTasks(syncRequest, syncContext);
                if (dispatcher.dispatching == EventDispatching.Send) {
                    dispatcher.SendQueuedEvents(); // use only for testing
                }
            }
            return new ExecuteSyncResult(response);
        }
    }
}
