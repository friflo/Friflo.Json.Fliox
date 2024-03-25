// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// Should not be public. commands are prefix with
    /// <b>std.*</b>
    internal static class Std  {
        // --- database
        public const string Echo                = "std.Echo";
        public const string Delay               = "std.Delay";
        public const string Containers          = "std.Containers";
        public const string Messages            = "std.Messages";
        public const string Schema              = "std.Schema";
        public const string Stats               = "std.Stats";
        public const string TransactionBegin    = "std.TransactionBegin";
        public const string TransactionCommit   = "std.TransactionCommit";
        public const string TransactionRollback = "std.TransactionRollback";
        public const string ExecuteRawSQL       = "std.ExecuteRawSQL";
        
        public const string Client              = "std.Client";

        // --- host
        public const string HostInfo            = "std.Host";
        public const string HostCluster         = "std.Cluster";
        
        // --- user
        public const string User                = "std.User";
    }
    
    public partial class DatabaseService
    {
        // ------------------------------ std command handler methods ------------------------------
        private void AddStdCommandHandlers() {
            // add each command handler individually
            // --- database
            AddCommandHandler      <JsonValue,   JsonValue>         (Std.Echo,              Echo);
            AddCommandHandlerAsync <int,         int>               (Std.Delay,             Delay);
            AddCommandHandlerAsync <Empty,       DbContainers>      (Std.Containers,        Containers);
            AddCommandHandler      <Empty,       DbMessages>        (Std.Messages,          Messages);
            AddCommandHandler      <Empty,       DbSchema>          (Std.Schema,            Schema);
            AddCommandHandlerAsync <string,      DbStats>           (Std.Stats,             Stats);
            AddCommandHandlerDual  <Empty,       TransactionResult> (Std.TransactionBegin,  TransactionBegin,     TransactionBeginAsync);
            AddCommandHandlerDual  <Empty,       TransactionResult> (Std.TransactionCommit, TransactionCommit,    TransactionCommitAsync);
            AddCommandHandlerDual  <Empty,       TransactionResult> (Std.TransactionRollback,TransactionRollback, TransactionRollbackAsync);
            AddCommandHandlerDual  <RawSql,      RawSqlResult>      (Std.ExecuteRawSQL,     ExecuteRawSQL,        ExecuteRawSQLAsync);
            // --- host
            AddCommandHandler      <HostParam,   HostInfo>          (Std.HostInfo,          HostInfo);
            AddCommandHandlerAsync <Empty,       HostCluster>       (Std.HostCluster,       HostCluster);
            // --- user
            AddCommandHandlerAsync <UserParam,   UserResult>        (Std.User,              User);
            // --- client
            AddCommandHandler      <ClientParam, ClientResult>      (Std.Client,            Client);
        }
        
        private static Result<JsonValue> Echo (Param<JsonValue> param, MessageContext context) {
            if (!param.Validate(out string error)) {
                return Result.Error(error);
            }
            return param.RawValue;
        }
        
        private static async Task<Result<int>> Delay (Param<int> param, MessageContext context) {
            if (!param.Get(out var delay, out var error)) {
                return Result.ValidationError(error);
            }
            var start = Stopwatch.GetTimestamp();
            await Task.Delay(delay).ConfigureAwait(false);
            
            var duration = (int)(1000 * (Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency);
            return duration;
        }
        
        private static Result<HostInfo> HostInfo (Param<HostParam> param, MessageContext context) {
            if (!param.Get(out var hostParam, out var error)) {
                return Result.ValidationError(error);
            }
            if (hostParam?.gcCollect == true) {
                GC.Collect();
            }
            var memory      = hostParam?.memory == true ? GetHostMemory() : null;
            var hub         = context.Hub;
            var pubSub      = hub.EventDispatcher != null;
            var info        = hub.Info;
            var host        = context.syncContext.Host as IHttpHost;
            var routes      = host?.Routes;
            var result      = new HostInfo {
                hostName        = hub.HostName,
                hostVersion     = hub.HostVersion,
                flioxVersion    = FlioxHub.FlioxVersion,
                projectName     = info.projectName,
                projectWebsite  = info.projectWebsite,
                envName         = info.envName,
                envColor        = info.envColor,
                pubSub          = pubSub,
                routes          = routes,
                memory          = memory
            };
            return result;
        }
        
        private static HostMemory GetHostMemory () {
#if UNITY_5_3_OR_NEWER || NETSTANDARD2_0 || NETSTANDARD2_1
            return new HostMemory {
                totalMemory                     = GC.GetTotalMemory(true),
            };
#else
            GCMemoryInfo mi = GC.GetGCMemoryInfo();
            var gcMemory = new HostGCMemory {
                highMemoryLoadThresholdBytes    = mi.HighMemoryLoadThresholdBytes,
                totalAvailableMemoryBytes       = mi.TotalAvailableMemoryBytes,
                memoryLoadBytes                 = mi.MemoryLoadBytes,
                heapSizeBytes                   = mi.HeapSizeBytes,
                fragmentedBytes                 = mi.FragmentedBytes,
            };
            return new HostMemory {
                gc                  = gcMemory,
                totalAllocatedBytes = GC.GetTotalAllocatedBytes(true),
                totalMemory         = GC.GetTotalMemory(true),
            };
#endif
        }

        private static async Task<Result<DbContainers>> Containers (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbContainers    = await database.GetDbContainers(database.name, context.Hub).ConfigureAwait(false);
            return dbContainers;
        }
        
        private static Result<DbMessages> Messages (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbMessages      = database.GetDbMessages();
            dbMessages.id       = database.name;
            return dbMessages;
        }
        
        private static Result<DbSchema> Schema (Param<Empty> param, MessageContext context) {
            var database    = context.Database;  
            return ClusterStore.CreateDbSchema(database);
        }
        
        private static async Task<Result<DbStats>> Stats (Param<string> param, MessageContext context) {
            var database        = context.Database;
            string[] containerNames;
            if (!param.GetValidate(out var containerName, out var error)) {
                return Result.ValidationError(error);
            }

            if (containerName == null) {
                var dbContainers    = await database.GetDbContainers(database.name, context.Hub).ConfigureAwait(false);
                containerNames      = dbContainers.containers;
            } else {
                containerNames = new [] { containerName };
            }
            var containerStats = new List<ContainerStats>();
            foreach (var name in containerNames) {
                var nameShort   = new ShortString(name);
                var container   = database.GetOrCreateContainer(nameShort);
                var aggregate   = new AggregateEntities { container = nameShort, type = AggregateType.count };
                var aggResult   = await container.AggregateEntitiesAsync(aggregate, context.syncContext).ConfigureAwait(false);
                
                double count    = aggResult.value ?? 0;
                var stats       = new ContainerStats { name = name, count = (long)count };
                containerStats.Add(stats);
            }
            var result = new DbStats { containers = containerStats.ToArray() };
            return result;
        }

        /// <summary>
        /// The returned result will be exchanged by <see cref="SyncContext.UpdateTransactionBeginResult"/>
        /// after execution of Begin or Rollback.
        /// </summary>
        private static async Task<Result<TransactionResult>> TransactionBeginAsync (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = await context.syncContext.TransactionAsync(TransCommand.Begin, taskIndex).ConfigureAwait(false);
            if (result.error == null) {
                return SyncTransaction.CreateResult(TransCommand.Rollback);
            }
            return Result.Error(result.error);
        }
        
        private static async Task<Result<TransactionResult>> TransactionCommitAsync (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = await context.syncContext.TransactionAsync(TransCommand.Commit, taskIndex).ConfigureAwait(false);
            if (result.error == null) {
                return SyncTransaction.CreateResult(result.state);
            }
            return Result.Error(result.error);
        }
        
        private static async Task<Result<TransactionResult>> TransactionRollbackAsync (Param<Empty> param, MessageContext context) {
            var taskIndex   = context.task.intern.index;
            var result      = await context.syncContext.TransactionAsync(TransCommand.Rollback, taskIndex).ConfigureAwait(false);
            if (result.error == null) {
                return SyncTransaction.CreateResult(TransCommand.Rollback);
            }
            return Result.Error(result.error);
        }
        
        private static async Task<Result<RawSqlResult>> ExecuteRawSQLAsync (Param<RawSql> param, MessageContext context) {
            if (!param.Validate(out string error)) {
                return Result.Error(error);
            }
            var sql = param.Value;
            if (sql == null) {
                return Result.Error("missing SQL command: E.g. { \"command\": \"select * from table_name;\" }");
            }
            var database    = context.Database;
            var result      = await database.ExecuteRawSQLAsync(sql, context.syncContext).ConfigureAwait(false);
            
            if (result.Success && sql.schema != true) {
                result.value.columns = null;
            }
            return result;
        }

        private static async Task<Result<HostCluster>> HostCluster (Param<Empty> param, MessageContext context) {
            return await ClusterStore.GetDbList(context).ConfigureAwait(false);
        }
        
        private static async Task<Result<UserResult>> User (Param<UserParam> param, MessageContext context) {
            if (!param.GetValidate(out UserParam options, out var error)) {
                return Result.ValidationError(error);
            }
            var user    = context.User;
            var groups  = user.GetGroups();
            
            if (options?.addGroups != null || options?.removeGroups != null) {
                var eventDispatcher  = context.Hub.EventDispatcher;
                if (eventDispatcher == null) {
                    return Result.Error("command requires a Hub with an EventDispatcher");
                }
                var authenticator = context.Hub.Authenticator;
                await authenticator.SetUserOptionsAsync(context.User, options).ConfigureAwait(false);
                
                eventDispatcher.UpdateSubUserGroups(user.userId.AsString(), groups);
            }
            
            var counts = new List<RequestCount>();
            ClusterUtils.CountsMapToList(counts, user.requestCounts, null);
            
            var clients = new List<string>(user.clients.Count);
            foreach (var clientPair in user.clients) {
                clients.Add(clientPair.Key.AsString());
            }
            var groupList   = groups.ToList();
            var roles       = user.roles != null ? user.roles.ToList() : new List<string>();
            return new UserResult { roles = roles, groups = groupList, counts = counts, clients = clients  };
        }
        
        /// <summary>
        /// Calling <see cref="Event.EventSubClient.SendUnacknowledgedEvents"/> here is too early.
        /// An outdated <see cref="Event.EventSubClient.eventReceiver"/> may be used.
        /// </summary>
        private static Result<ClientResult> Client (Param<ClientParam> param, MessageContext context) {
            /* if (context.ClientId.IsNull()) {
                return context.Error<ClientResult>("Missing client id (clt)");
            } */
            if (!param.GetValidate(out var clientParam, out string error)) { 
                return Result.ValidationError(error);
            }
            error = EnsureClientId(clientParam, context);
            if (error != null) {
                return Result.Error(error);
            }
            error = SetQueueEvents(clientParam, context);
            if (error != null) {
                return Result.Error(error);
            }
            var hub         = context.Hub;
            var dispatcher  = hub.EventDispatcher;
            var result      = new ClientResult { clientId = context.ClientId.AsString() };
            if (dispatcher != null && !context.ClientId.IsNull() && dispatcher.TryGetSubscriber(context.ClientId, out var client)) {
                result.queueEvents          = client.queueEvents;
                result.queuedEvents         = client.QueuedEventsCount;
                result.subscriptionEvents   = ClusterUtils.GetSubscriptionEvents(dispatcher, client, default);
                /* if (clientParam != null && clientParam.syncEvents) {
                    client.SendUnacknowledgedEvents(); see comment above
                } */
            }
            return result;
        }
        
        private static string EnsureClientId(ClientParam clientParam, MessageContext context) {
            if (clientParam?.ensureClientId != true)
                return null;
            var hub         = context.Hub;
            var dispatcher  = hub.EventDispatcher;
            if (dispatcher == null) {
                return "std.Client ensureClientId requires an EventDispatcher assigned to FlioxHub";
            }
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, context.syncContext, out var error)) {
                return error;
            }
            return null;
        }
        
        private static string SetQueueEvents(ClientParam clientParam, MessageContext context) {
            var queueEvents = clientParam?.queueEvents;
            if (queueEvents == null)
                return null;
            var hub         = context.Hub;
            var dispatcher  = hub.EventDispatcher;
            if (dispatcher == null) {
                return "std.Client queueEvents requires an EventDispatcher assigned to FlioxHub";
            }

            if (queueEvents.Value) {
                var syncContext = context.syncContext; 
                if (!syncContext.authState.hubPermission.queueEvents) {
                    return "std.Client queueEvents requires permission (Role.hubRights) queueEvents = true";
                }
                if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, syncContext, out string error)) {
                    return error;
                }
                var client = dispatcher.GetOrCreateSubClient(syncContext.User, syncContext.clientId, syncContext.eventReceiver);
                client.queueEvents = true;
                return null;
            } else {
                if (dispatcher.TryGetSubscriber(context.ClientId, out var client)) {
                    client.queueEvents = false;
                }
            }
            return null;
        }
    }
}