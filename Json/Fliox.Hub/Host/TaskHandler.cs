// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToConstant.Local
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Host
{
    public  delegate  void    HostMessageHandler<TParam>              (Param<TParam> param, MessageContext context);
    public  delegate  TResult HostCommandHandler<TParam, out TResult> (Param<TParam> param, MessageContext context);

    /// <summary>
    /// A <see cref="TaskHandler"/> is attached to every <see cref="EntityDatabase"/> to handle all
    /// <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
    /// <br/>
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
    /// </summary>
    public class TaskHandler
    {
        private readonly Dictionary<string, MessageDelegate> messages = new Dictionary<string, MessageDelegate>();
        
        public TaskHandler () {
            // AddUsingCommandHandler();
            // add each command handler individually
            // --- database
            AddCommandHandler      <JsonValue,   JsonValue>    (Std.Echo,         Echo);
            AddCommandHandlerAsync <Empty,       DbContainers> (Std.Containers,   Containers);
            AddCommandHandler      <Empty,       DbMessages>   (Std.Messages,     Messages);
            AddCommandHandler      <Empty,       DbSchema>     (Std.Schema,       Schema);
            AddCommandHandlerAsync <string,      DbStats>      (Std.Stats,        Stats);
            // --- host
            AddCommandHandler      <Empty,       HostDetails>  (Std.HostDetails,  Details);
            AddCommandHandlerAsync <Empty,       HostCluster>  (Std.HostCluster,  Cluster);
        }
        
        /// <summary>
        /// Add a synchronous message handler method with a method signature like:
        /// <code>
        /// void TestMessage(Param&lt;string&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandler<TParam> (string name, HostMessageHandler<TParam> handler) {
            var command = new MessageDelegate<TParam>(name, handler);
            messages.Add(name, command);
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
            messages.Add(name, command);
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
            messages.Add(name, command);
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
        ///     Commonly the instance of a <see cref="TaskHandler"/></param>
        /// <param name="commandPrefix">the prefix of a message/command - e.g. "test."; null or "" to add messages without prefix</param>
        protected void AddMessageHandlers<TClass>(TClass instance, string commandPrefix) where TClass : class
        {
            var type                = instance.GetType();
            var handlers            = TaskHandlerUtils.GetHandlers(type);
            if (handlers == null)
                return;

            foreach (var handler in handlers) {
                MessageDelegate messageDelegate;
                if (handler.resultType == typeof(void)) {
                    messageDelegate = CreateMessageCallback(instance, handler);
                } else {
                    messageDelegate = CreateCommandCallback(instance, handler);
                }
                var name            = string.IsNullOrEmpty(commandPrefix) ? handler.name : $"{commandPrefix}{handler.name}";
                messages.Add(name, messageDelegate);
            }
        }
        
        private static MessageDelegate  CreateMessageCallback<TClass>(TClass handlerClass, HandlerInfo handler) where TClass : class {
            var genericArgs         = new Type[1];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            var genericTypeArgs     = typeof(HostMessageHandler<>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = handler.name;
            constructorParams[1]    = handlerDelegate;
            object instance = TypeMapperUtils.CreateGenericInstance(typeof(MessageDelegate<>),      genericArgs, constructorParams);   
            return (MessageDelegate)instance;
        }
        
        private static MessageDelegate  CreateCommandCallback<TClass>(TClass handlerClass, HandlerInfo handler) where TClass : class
        {
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            genericArgs[1]          = handler.resultType;
            var genericTypeArgs     = typeof(HostCommandHandler<,>).MakeGenericType(genericArgs);
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = handler.name;
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
            return param.JsonParam;
        }
        
        private static HostDetails Details (Param<Empty> param, MessageContext context) {
            var hub             = context.Hub;
            var info            = hub.Info;
            var details         = new HostDetails {
                version         = hub.Version,
                hostName        = hub.hostName,
                projectName     = info?.projectName,
                projectWebsite  = info?.projectWebsite,
                envName         = info?.envName,
                envColor        = info?.envColor
            };
            return details;
        }

        private static async Task<DbContainers> Containers (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = context.DatabaseName ?? EntityDatabase.MainDB;
            return dbContainers;
        }
        
        private static DbMessages Messages (Param<Empty> param, MessageContext context) {
            var database        = context.Database;  
            var dbMessages      = database.GetDbMessages();
            dbMessages.id       = context.DatabaseName ?? EntityDatabase.MainDB;
            return dbMessages;
        }
        
        private static DbSchema Schema (Param<Empty> param, MessageContext context) {
            context.WritePretty = false;
            var database        = context.Database;  
            var databaseName    = context.DatabaseName ?? EntityDatabase.MainDB;
            return ClusterStore.CreateCatalogSchema(database, databaseName);
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
                var aggResult   = await container.AggregateEntities(aggregate, context.ExecuteContext).ConfigureAwait(false);
                
                double count    = aggResult.value ?? 0;
                var stats       = new ContainerStats { name = name, count = (long)count };
                containerStats.Add(stats);
            }
            var result = new DbStats { containers = containerStats.ToArray() };
            return result;
        }
        
        private static async Task<HostCluster> Cluster (Param<Empty> param, MessageContext context) {
            var hub             = context.Hub;
            return await ClusterStore.GetDbList(hub).ConfigureAwait(false);
        }
        
        // --- internal API ---
        internal bool TryGetMessage(string name, out MessageDelegate message) {
            return messages.TryGetValue(name, out message);
        }
        
        internal string[] GetMessages() {
            var count = CountMessageTypes(MsgType.Message);
            var result = new string[count];
            int n = 0;
            foreach (var pair in messages) {
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
            AddCommands(result, ref n, false, messages);
            AddCommands(result, ref n, true,  messages);
            return result;
        }
        
        private int CountMessageTypes (MsgType msgType) {
            int count = 0;
            foreach (var pair in messages) {
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

        protected static bool AuthorizeTask(SyncRequestTask task, ExecuteContext executeContext, out SyncTaskResult error) {
            var authorizer = executeContext.authState.authorizer;
            if (authorizer.Authorize(task, executeContext)) {
                error = null;
                return true;
            }
            var sb = new StringBuilder(); // todo StringBuilder could be pooled
            sb.Append("not authorized");
            var authError = executeContext.authState.error; 
            if (authError != null) {
                sb.Append(". ");
                sb.Append(authError);
            }
            var anonymous = executeContext.hub.Authenticator.anonymousUser;
            var user = executeContext.User;
            if (user != anonymous) {
                sb.Append(". user: ");
                sb.Append(user.userId);
            }
            var message = sb.ToString();
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            if (AuthorizeTask(task, executeContext, out var error)) {
                var result = await task.Execute(database, response, executeContext).ConfigureAwait(false);
                return result;
            }
            return error;
        }
    }
}