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
    public delegate TResult CommandHandler<TParam, out TResult>(Command<TParam> command);

    /// <summary>
    /// A <see cref="TaskHandler"/> is attached to every <see cref="EntityDatabase"/> to handle all
    /// <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
    /// <br/>
    /// Each task is either a database operation or a custom command.
    /// <list type="bullet">
    ///   <item>
    ///     <b>Database operations</b> are a build-in functionality of every <see cref="EntityDatabase"/>.
    ///     These operations are:
    ///     <see cref="CreateEntities"/>, <see cref="UpsertEntities"/>, <see cref="DeleteEntities"/>,
    ///     <see cref="PatchEntities"/>, <see cref="ReadEntities"/> or <see cref="QueryEntities"/>.
    ///   </item>
    ///   <item>
    ///     <b>Custom commands</b> are added by an application to perform custom operations.
    ///     Each command is a tuple of its name and its command value. See <see cref="SendCommand"/>.
    ///     When executed by its handler method it returns a command result. See <see cref="SendCommandResult"/>. 
    ///   </item>
    /// </list>  
    /// </summary>
    public class TaskHandler
    {
        private readonly Dictionary<string, CommandCallback> commands = new Dictionary<string, CommandCallback>();
        
        private static bool _oldStyleUsage = false;
        
        public TaskHandler () {
            if (_oldStyleUsage) {
                // --- Db*
                AddCommandHandler       (StdCommand.DbEcho,        new CommandHandler<JsonValue, JsonValue>         (DbEcho));
                AddCommandHandlerAsync  (StdCommand.DbContainers,  new CommandHandler<Empty,     Task<DbContainers>>(DbContainers));
                AddCommandHandler       (StdCommand.DbCommands,    new CommandHandler<Empty,     DbCommands>        (DbCommands));
                AddCommandHandler       (StdCommand.DbSchema,      new CommandHandler<Empty,     DbSchema>          (DbSchema));
                AddCommandHandlerAsync  (StdCommand.DbStats,       new CommandHandler<string,    Task<DbStats>>     (DbStats));
                // --- Hub*
                AddCommandHandler       (StdCommand.HubInfo,       new CommandHandler<Empty,     HubInfo>           (HubInfo));
                AddCommandHandlerAsync  (StdCommand.HubCluster,    new CommandHandler<Empty,     Task<HubCluster>>  (HubCluster));
            }
            // --- Db*
            AddCommand      <JsonValue,   JsonValue>    (nameof(DbEcho),        DbEcho);
            AddCommandAsync <Empty,       DbContainers> (nameof(DbContainers),  DbContainers);
            AddCommand      <Empty,       DbCommands>   (nameof(DbCommands),    DbCommands);
            AddCommand      <Empty,       DbSchema>     (nameof(DbSchema),      DbSchema);
            // --- Hub*
            AddCommand      <Empty,       HubInfo>      (nameof(HubInfo),       HubInfo);
            AddCommandAsync <Empty,       HubCluster>   (nameof(HubCluster),    HubCluster);
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// private static string TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommand<TParam, TResult> (string name, Func<Command<TParam>, TResult> method) {
            AddCommandHandler (name, new CommandHandler<TParam, TResult> (method));
        }
        
        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// private static Task&lt;string&gt; TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandAsync<TParam, TResult> (string name, Func<Command<TParam>, Task<TResult>> method) {
            AddCommandHandlerAsync (name, new CommandHandler<TParam, Task<TResult>> (method));
        }
       
        /// <summary>
        /// Add all methods using <see cref="Command{TParam}"/> as a single parameter as a command handler.
        /// E.g.
        /// <code>
        /// private static string TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// Command handler methods can be: <br/>
        /// - static or instance methods <br/>
        /// - synchronous or asynchronous - using <see cref="Task{TResult}"/> as return type.
        /// </summary>
        protected void AddCommandHandlers()
        {
            var type                = GetType();
            var handlers            = TaskHandlerUtils.GetHandlers(type);
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            foreach (var handler in handlers) {
                // if (handler.name == "DbContainers") { int i = 1; }
                genericArgs[0]      = handler.valueType;
                genericArgs[1]      = handler.resultType;
                var genericTypeArgs = typeof(CommandHandler<,>).MakeGenericType(genericArgs);
                var firstArgument   = handler.method.IsStatic ? null : this;
                var handlerDelegate = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

                constructorParams[0]    = handler.name;
                constructorParams[1]    = handlerDelegate;
                object instance;
                // is return type of command handler of type: Task<TResult> ?  (==  is async command handler)
                if (handler.resultTaskType != null) {
                    genericArgs[1] = handler.resultTaskType;
                    instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandAsyncCallback<,>), genericArgs, constructorParams);
                } else {
                    instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandCallback<,>),      genericArgs, constructorParams);    
                }
                var commandCallback = (CommandCallback)instance;
                commands.Add(handler.name, commandCallback);
            }
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static JsonValue DbEcho (Command<JsonValue> command) {
            return command.JsonParam;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static HubInfo HubInfo (Command<Empty> command) {
            var hub     = command.Hub;
            var info    = new HubInfo {
                version     = hub.Version,
                hostName    = hub.hostName,
                label       = hub.description,
                website     = hub.website
            };
            return info;
        }

        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static async Task<DbContainers> DbContainers (Command<Empty> command) {
            var database        = command.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbContainers;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static DbCommands DbCommands (Command<Empty> command) {
            var database        = command.Database;  
            var dbCommands      = database.GetDbCommands();
            dbCommands.id       = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbCommands;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static DbSchema DbSchema (Command<Empty> command) {
            var database        = command.Database;  
            var databaseName    = command.DatabaseName ?? EntityDatabase.MainDB;
            return ClusterStore.CreateCatalogSchema(database, databaseName);
        }
        
        internal static async Task<DbStats> DbStats (Command<string> command) {
            var database        = command.Database;
            var containerName   = command.Param;
            var container       = database.GetOrCreateContainer(containerName);
            var aggregate       = new AggregateEntities { container = containerName };
            var aggResult       = await container.AggregateEntities(aggregate, command.MessageContext);
            
            var count           = aggResult.counts["*"];
            var result          = new DbStats { count = count };
            return result;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static async Task<HubCluster> HubCluster (Command<Empty> command) {
            var hub = command.Hub;
            return await ClusterStore.GetDbList(hub).ConfigureAwait(false);
        }
        
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command); 
        }
        
        private void AddCommandHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        private void AddCommandHandlerAsync<TValue, TResult>(string name, CommandHandler<TValue, Task<TResult>> handler) {
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        internal string[] GetCommands() {
            var result = new string[commands.Count];
            int n = 0;
            foreach (var pair in commands) { result[n++] = pair.Key; }
            return result;
        }


        
        protected static bool AuthorizeTask(SyncRequestTask task, MessageContext messageContext, out SyncTaskResult error) {
            var authorizer = messageContext.authState.authorizer;
            if (authorizer.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var sb = new StringBuilder(); // todo StringBuilder could be pooled
            sb.Append("not authorized");
            var authError = messageContext.authState.error; 
            if (authError != null) {
                sb.Append(". ");
                sb.Append(authError);
            }
            var anonymous = messageContext.hub.Authenticator.anonymousUser;
            var user = messageContext.User;
            if (user != anonymous) {
                sb.Append(". user: ");
                sb.Append(user.userId);
            }
            var message = sb.ToString();
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (AuthorizeTask(task, messageContext, out var error)) {
                var result = await task.Execute(database, response, messageContext).ConfigureAwait(false);
                return result;
            }
            return error;
        }
    }
}