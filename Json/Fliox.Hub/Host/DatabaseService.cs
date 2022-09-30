// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToConstant.Local
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host
{
    public  delegate  void    HostMessageHandler<TParam>              (Param<TParam> param, MessageContext context);
    public  delegate  Task    HostMessageHandlerAsync<TParam>         (Param<TParam> param, MessageContext context);
    public  delegate  TResult HostCommandHandler<TParam, out TResult> (Param<TParam> param, MessageContext context);

    /// <summary>
    /// A <see cref="DatabaseService"/> is attached to every <see cref="EntityDatabase"/> to handle all
    /// <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
    /// </summary>
    /// <remarks>
    /// Each task is either a database operation, a command or a message.
    /// <list type="bullet">
    ///   <item>
    ///     <b>Database operations</b> are a build-in functionality of every <see cref="EntityDatabase"/>.
    ///     These operations are:
    ///     <see cref="CreateEntities"/>, <see cref="UpsertEntities"/>, <see cref="DeleteEntities"/>,
    ///     <see cref="PatchEntities"/>, <see cref="ReadEntities"/> or <see cref="QueryEntities"/>.
    ///   </item>
    ///   <item>
    ///     An application can add <b>command</b> handlers to perform custom operations.
    ///     Each command is a tuple of its name and param. See <see cref="SendCommand"/>.
    ///     Its command handler must return a result. See <see cref="SendCommandResult"/>. 
    ///   </item>
    ///   <item>
    ///     Similar to commands an application can add <b>message</b> handlers to process events or notifications.
    ///     Each message is a tuple of its name and param. See <see cref="SendMessage"/>.
    ///     In contrast to commands message handlers return void (nothing).
    ///   </item>
    /// </list>
    /// </remarks>
    public class DatabaseService
    {
        [DebuggerBrowsable(Never)]
        private readonly  Dictionary<string, MessageDelegate>   handlers = new Dictionary<string, MessageDelegate>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private           IReadOnlyCollection<MessageDelegate>  Handlers => handlers.Values;
        
        public DatabaseService () {
            // AddUsingCommandHandler();
            // add each command handler individually
            // --- database
            AddCommandHandler      <JsonValue,   JsonValue>     (Std.Echo,          Echo);
            AddCommandHandlerAsync <Empty,       DbContainers>  (Std.Containers,    Containers);
            AddCommandHandler      <Empty,       DbMessages>    (Std.Messages,      Messages);
            AddCommandHandler      <Empty,       DbSchema>      (Std.Schema,        Schema);
            AddCommandHandlerAsync <string,      DbStats>       (Std.Stats,         Stats);
            // --- host
            AddCommandHandler      <HostParam,   HostInfo>      (Std.HostInfo,      HostInfo);
            AddCommandHandlerAsync <Empty,       HostCluster>   (Std.HostCluster,   HostCluster);
            // --- user
            AddCommandHandlerAsync <UserParam,   UserResult>    (Std.User,          User);
            // --- client
            AddCommandHandler      <ClientParam, ClientResult>  (Std.Client,        Client);
        }
        
        /// <summary>
        /// Add a synchronous message handler method with a method signature like:
        /// <code>
        /// void TestMessage(Param&lt;string&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandler<TParam> (string name, HostMessageHandler<TParam> handler) {
            var message = new MessageDelegate<TParam>(name, handler);
            handlers.Add(name, message);
        }
        
        /// <summary>
        /// Add an asynchronous message handler method with a method signature like:
        /// <code>
        /// Task TestMessage(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandlerAsync<TParam> (string name, HostMessageHandlerAsync<TParam> handler) {
            var message = new MessageAsyncDelegate<TParam>(name, handler);
            handlers.Add(name, message);
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandler<TParam, TResult> (string name, HostCommandHandler<TParam, TResult> handler) {
            var command = new CommandDelegate<TParam, TResult>(name, handler);
            handlers.Add(name, command);
        }

        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// Task&lt;bool&gt; TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandlerAsync<TParam, TResult> (string name, HostCommandHandler<TParam, Task<TResult>> handler) {
            var command = new CommandAsyncDelegate<TParam, TResult>(name, handler);
            handlers.Add(name, command);
        }
       
        /// <summary>
        /// Add all methods of the given class <paramref name="instance"/> with the parameters <br/>
        /// (<see cref="Param{TParam}"/> param, <see cref="MessageContext"/> context) as a message/command handler. <br/>
        /// A command handler has return type - a message handler returns void. <br/>
        /// Command handler example:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// Message handler methods can be: <br/>
        /// - static or instance methods <br/>
        /// - synchronous or asynchronous - using <see cref="Task{TResult}"/> as return type.
        /// </summary>
        /// <param name="instance">the instance of class containing message handler methods.
        ///     Commonly the instance of a <see cref="DatabaseService"/></param>
        /// <param name="messagePrefix">the prefix of a message/command - e.g. "test."; null or "" to add messages without prefix</param>
        protected void AddMessageHandlers<TClass>(TClass instance, string messagePrefix) where TClass : class
        {
            var type                = typeof(TClass);
            var handlerInfos        = DatabaseServiceUtils.GetHandlers(type);
            if (handlerInfos == null)
                return;

            foreach (var handler in handlerInfos) {
                MessageDelegate messageDelegate;
                if (handler.resultType == typeof(void)) {
                    messageDelegate = CreateMessageCallback(instance, handler, messagePrefix);
                } else {
                    messageDelegate = CreateCommandCallback(instance, handler, messagePrefix);
                }
                handlers.Add(messageDelegate.name, messageDelegate);
            }
        }
        
        private static string GetHandlerName(HandlerInfo handler, string messagePrefix) {
            if (string.IsNullOrEmpty(messagePrefix))
                return handler.name;
            return $"{messagePrefix}{handler.name}";
        }
        
        private static MessageDelegate  CreateMessageCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[1];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            var genericTypeArgs     = typeof(HostMessageHandler<>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance = TypeMapperUtils.CreateGenericInstance(typeof(MessageDelegate<>),      genericArgs, constructorParams);   
            return (MessageDelegate)instance;
        }
        
        private static MessageDelegate  CreateCommandCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            genericArgs[1]          = handler.resultType;
            var genericTypeArgs     = typeof(HostCommandHandler<,>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance;
            // is return type of command handler of type: Task<TResult> ?  (==  is async command handler)
            if (handler.resultTaskType != null) {
                genericArgs[1] = handler.resultTaskType;
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandAsyncDelegate<,>), genericArgs, constructorParams);
            } else {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandDelegate<,>),      genericArgs, constructorParams);    
            }
            return (MessageDelegate)instance;
        }
        
        // ------------------------------ command handler methods ------------------------------
        private static JsonValue Echo (Param<JsonValue> param, MessageContext context) {
            return param.RawParam;
        }
        
        private static HostInfo HostInfo (Param<HostParam> param, MessageContext context) {
            if (!param.Get(out var hostParam, out var error)) {
                return context.Error<HostInfo>(error);
            }
            if (hostParam?.gcCollect == true) {
                GC.Collect();
            }
            var memory  = hostParam?.memory == true ? GetHostMemory() : null;
            var hub     = context.Hub;
            var pubSub  = hub.EventDispatcher != null;
            var info    = hub.Info;
            var routes  = new List<string>(hub.Routes);
            var result  = new HostInfo {
                hostVersion     = hub.HostVersion,
                flioxVersion    = FlioxHub.FlioxVersion,
                hostName        = hub.hostName,
                projectName     = info?.projectName,
                projectWebsite  = info?.projectWebsite,
                envName         = info?.envName,
                envColor        = info?.envColor,
                pubSub          = pubSub,
                routes          = routes,
                memory          = memory
            };
            return result;
        }
        
        private static HostMemory GetHostMemory () {
#if UNITY_5_3_OR_NEWER
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

        private static async Task<DbContainers> Containers (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = context.DatabaseName;
            return dbContainers;
        }
        
        private static DbMessages Messages (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbMessages      = database.GetDbMessages();
            dbMessages.id       = context.DatabaseName;
            return dbMessages;
        }
        
        private static DbSchema Schema (Param<Empty> param, MessageContext context) {
            context.WritePretty = false;
            var database        = context.Database;  
            var databaseName    = context.DatabaseName;
            return ClusterStore.CreateDbSchema(database, databaseName);
        }
        
        private static async Task<DbStats> Stats (Param<string> param, MessageContext context) {
            var database        = context.Database;
            string[] containerNames;
            if (!param.GetValidate(out var containerName, out var error))
                return context.Error<DbStats>(error);

            if (containerName == null) {
                var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
                containerNames      = dbContainers.containers;
            } else {
                containerNames = new [] { containerName };
            }
            var containerStats = new List<ContainerStats>();
            foreach (var name in containerNames) {
                var container   = database.GetOrCreateContainer(name);
                var aggregate   = new AggregateEntities { container = name, type = AggregateType.count };
                var aggResult   = await container.AggregateEntities(aggregate, context.SyncContext).ConfigureAwait(false);
                
                double count    = aggResult.value ?? 0;
                var stats       = new ContainerStats { name = name, count = (long)count };
                containerStats.Add(stats);
            }
            var result = new DbStats { containers = containerStats.ToArray() };
            return result;
        }
        
        private static async Task<HostCluster> HostCluster (Param<Empty> param, MessageContext context) {
            return await ClusterStore.GetDbList(context).ConfigureAwait(false);
        }
        
        private static async Task<UserResult> User (Param<UserParam> param, MessageContext context) {
            if (!param.GetValidate(out UserParam options, out var error)) {
                return context.Error<UserResult>(error);
            }
            var user    = context.User;
            var groups  = user.GetGroups();
            
            if (options?.addGroups != null || options?.removeGroups != null) {
                var eventDispatcher  = context.Hub.EventDispatcher;
                if (eventDispatcher == null) {
                    return context.Error<UserResult>("command requires a Hub with an EventDispatcher");
                }
                var authenticator = context.Hub.Authenticator;
                await authenticator.SetUserOptions(context.User, options).ConfigureAwait(false);
                
                eventDispatcher.UpdateSubUserGroups(user.userId, groups);
            }
            
            var counts = new List<RequestCount>();
            ClusterUtils.CountsMapToList(counts, user.requestCounts, null);
            
            var clients = new List<JsonKey>();
            foreach (var clientPair in user.clients) {
                clients.Add(clientPair.Key);
            }
            return new UserResult { groups = groups.ToArray(), counts = counts, clients = clients };
        }
        
        /// <summary>
        /// Calling <see cref="Event.EventSubClient.SendUnacknowledgedEvents"/> here is too early.
        /// An outdated <see cref="Event.EventSubClient.eventReceiver"/> may be used.
        /// </summary>
        private static ClientResult Client (Param<ClientParam> param, MessageContext context) {
            /* if (context.ClientId.IsNull()) {
                return context.Error<ClientResult>("Missing client id (clt)");
            } */
            if (!param.GetValidate(out var clientParam, out string error)) {
                return context.Error<ClientResult>(error);
            }
            error = EnsureClientId(clientParam, context);
            if (error != null) {
                return context.Error<ClientResult>(error);
            }
            error = SetQueueEvents(clientParam, context);
            if (error != null) {
                return context.Error<ClientResult>(error);
            }
            var hub         = context.Hub;
            var dispatcher  = hub.EventDispatcher;
            var result      = new ClientResult { clientId = context.ClientId };
            if (dispatcher != null && !context.ClientId.IsNull() && dispatcher.TryGetSubscriber(context.ClientId, out var client)) {
                result.queueEvents          = client.queueEvents;
                result.queuedEvents         = client.QueuedEventsCount;
                result.subscriptionEvents   = ClusterUtils.GetSubscriptionEvents(client, default);
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
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, context.SyncContext, out var error)) {
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
                var syncContext = context.SyncContext; 
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
        
        // --- internal API ---
        internal bool TryGetMessage(string name, out MessageDelegate message) {
            return handlers.TryGetValue(name, out message);
        }
        
        internal string[] GetMessages() {
            var count = CountMessageTypes(MsgType.Message);
            var result = new string[count];
            int n = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == MsgType.Message)
                    result[n++] = pair.Key;
            }
            return result;
        }
        
        internal string[] GetCommands() {
            var count = CountMessageTypes(MsgType.Command);
            var result = new string[count];
            int n = 0;
            // add std. commands on the bottom
            AddCommands(result, ref n, false, handlers);
            AddCommands(result, ref n, true,  handlers);
            return result;
        }
        
        private int CountMessageTypes (MsgType msgType) {
            int count = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == msgType) count++;
            }
            return count;
        }
        
        private static void AddCommands (string[] commands, ref int n, bool standard, Dictionary<string, MessageDelegate> commandMap) {
            foreach (var pair in commandMap) {
                if (pair.Value.MsgType != MsgType.Command)
                    continue;
                var name = pair.Key;
                if (name.StartsWith("std.") == standard)
                    commands[n++] = name;
            }
        }

        protected static bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext, out SyncTaskResult error) {
            var taskAuthorizer = syncContext.authState.taskAuthorizer;
            if (taskAuthorizer.AuthorizeTask(task, syncContext)) {
                error = null;
                return true;
            }
            var sb = new StringBuilder(); // todo StringBuilder could be pooled
            sb.Append("not authorized");
            var authError = syncContext.authState.error; 
            if (authError != null) {
                sb.Append(". ");
                sb.Append(authError);
            }
            var anonymous = syncContext.hub.Authenticator.anonymousUser;
            var user = syncContext.User;
            if (user != anonymous) {
                sb.Append(". user: ");
                sb.Append(user.userId);
            }
            var message = sb.ToString();
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (AuthorizeTask(task, syncContext, out var error)) {
                var result = await task.Execute(database, response, syncContext).ConfigureAwait(false);
                return result;
            }
            return error;
        }
    }
}