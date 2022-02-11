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

// ReSharper disable MemberCanBeProtected.Global
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
            string db   = "db";
            string host = "host";
            if (_oldStyleUsage) {
                // --- db.*
                AddCommandHandler       (StdCommand.DbEcho,         db, new CommandHandler<JsonValue, JsonValue>         (Echo));
                AddCommandHandlerAsync  (StdCommand.DbContainers,   db, new CommandHandler<Empty,     Task<DbContainers>>(Containers));
                AddCommandHandler       (StdCommand.DbCommands,     db, new CommandHandler<Empty,     DbCommands>        (Commands));
                AddCommandHandler       (StdCommand.DbSchema,       db, new CommandHandler<Empty,     DbSchema>          (Schema));
                AddCommandHandlerAsync  (StdCommand.DbStats,        db, new CommandHandler<string,    Task<DbStats>>     (Stats));
                // --- host.*
                AddCommandHandler       (StdCommand.HostDetails,    host, new CommandHandler<Empty,   HostDetails>       (Details));
                AddCommandHandlerAsync  (StdCommand.HostCluster,    host, new CommandHandler<Empty,   Task<HostCluster>> (Cluster));
            }
            // --- db.*
            AddCommand      <JsonValue,   JsonValue>    (nameof(Echo),      db,     Echo);
            AddCommandAsync <Empty,       DbContainers> (nameof(Containers),db,     Containers);
            AddCommand      <Empty,       DbCommands>   (nameof(Commands),  db,     Commands);
            AddCommand      <Empty,       DbSchema>     (nameof(Schema),    db,     Schema);
            AddCommandAsync <string,      DbStats>      (nameof(Stats),     db,     Stats);
            // --- host.*
            AddCommand      <Empty,       HostDetails>  (nameof(Details),   host,   Details);
            AddCommandAsync <Empty,       HostCluster>  (nameof(Cluster),   host,   Cluster);
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// private static string TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        public void AddCommand<TParam, TResult> (string name, string domain, Func<Command<TParam>, TResult> method) {
            AddCommandHandler (name, domain, new CommandHandler<TParam, TResult> (method));
        }
        
        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// private static Task&lt;string&gt; TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        public void AddCommandAsync<TParam, TResult> (string name, string domain, Func<Command<TParam>, Task<TResult>> method) {
            AddCommandHandlerAsync (name, domain, new CommandHandler<TParam, Task<TResult>> (method));
        }
       
        /// <summary>
        /// Add all methods of the given <see cref="handlerClass"/> using <see cref="Command{TParam}"/> as a
        /// single parameter as a command handler.
        /// E.g.
        /// <code>
        /// private string TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// Command handler methods can be: <br/>
        /// - static or instance methods <br/>
        /// - synchronous or asynchronous - using <see cref="Task{TResult}"/> as return type.
        /// </summary>
        /// <param name="handlerClass">the instance of class containing command handler methods.
        ///     Commonly the instance of a <see cref="TaskHandler"/></param>
        /// <param name="commandPrefix">the prefix of a command - e.g. "test."; null or "" to add commands without prefix</param>
        public void AddCommandHandlers<TClass>(TClass handlerClass, string commandPrefix) where TClass : class
        {
            var type                = handlerClass.GetType();
            var handlers            = TaskHandlerUtils.GetHandlers(type);
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            foreach (var handler in handlers) {
                // if (handler.name == "DbContainers") { int i = 1; }
                genericArgs[0]      = handler.valueType;
                genericArgs[1]      = handler.resultType;
                var genericTypeArgs = typeof(CommandHandler<,>).MakeGenericType(genericArgs);
                var firstArgument   = handler.method.IsStatic ? null : handlerClass;
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
                var name = string.IsNullOrEmpty(commandPrefix) ? handler.name : $"{commandPrefix}{handler.name}";
                commands.Add(name, commandCallback);
            }
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static JsonValue Echo (Command<JsonValue> command) {
            return command.JsonParam;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static HostDetails Details (Command<Empty> command) {
            var hub     = command.Hub;
            var details = new HostDetails {
                version     = hub.Version,
                hostName    = hub.hostName,
                label       = hub.description,
                website     = hub.website
            };
            return details;
        }

        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static async Task<DbContainers> Containers (Command<Empty> command) {
            var database        = command.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbContainers;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static DbCommands Commands (Command<Empty> command) {
            var database        = command.Database;  
            var dbCommands      = database.GetDbCommands();
            dbCommands.id       = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbCommands;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static DbSchema Schema (Command<Empty> command) {
            var database        = command.Database;  
            var databaseName    = command.DatabaseName ?? EntityDatabase.MainDB;
            return ClusterStore.CreateCatalogSchema(database, databaseName);
        }
        
        internal static async Task<DbStats> Stats (Command<string> command) {
            var database        = command.Database;
            string[] containerNames;
            var containerName   = command.Param;
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
                var aggResult   = await container.AggregateEntities(aggregate, command.MessageContext).ConfigureAwait(false);
                
                double count    = aggResult.value ?? 0;
                var stats       = new ContainerStats { name = name, count = (long)count };
                containerStats.Add(stats);
            }
            var result = new DbStats { containers = containerStats.ToArray() };
            return result;
        }
        
        /// must not be private so <see cref="TaskHandlerUtils.GetHandlers"/> finds it
        internal static async Task<HostCluster> Cluster (Command<Empty> command) {
            var hub = command.Hub;
            return await ClusterStore.GetDbList(hub).ConfigureAwait(false);
        }
        
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command);
        }
        
        private void AddCommandHandler<TValue, TResult>(string name, string domain, CommandHandler<TValue, TResult> handler) {
            name = string.IsNullOrEmpty(domain) ? name : $"{domain}.{name}";
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        private void AddCommandHandlerAsync<TValue, TResult>(string name, string domain, CommandHandler<TValue, Task<TResult>> handler) {
            name = string.IsNullOrEmpty(domain) ? name : $"{domain}.{name}";
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