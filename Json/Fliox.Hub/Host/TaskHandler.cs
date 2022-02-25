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
    internal delegate TResult CmdHandler<TParam, out TResult>(Command<TParam> command);

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
        
        public TaskHandler () {
            // AddUsingCommandHandler();
            // add each command handler individually
            // --- database
            AddCommand      <JsonValue,   JsonValue>    (Std.Echo,         Echo);
            AddCommandAsync <Empty,       DbContainers> (Std.Containers,   Containers);
            AddCommand      <Empty,       DbCommands>   (Std.Commands,     Commands);
            AddCommand      <Empty,       DbSchema>     (Std.Schema,       Schema);
            AddCommandAsync <string,      DbStats>      (Std.Stats,        Stats);
            // --- host
            AddCommand      <Empty,       HostDetails>  (Std.HostDetails,  Details);
            AddCommandAsync <Empty,       HostCluster>  (Std.HostCluster,  Cluster);
        }
        
        //  ReSharper disable once UnusedMember.Local
        /// keep implementation to show how to add command handler using new <see cref="CmdHandler{TParam,TResult}"/>
        private void AddUsingCommandHandler() {
            // --- database
            AddCommandHandler       (Std.Echo,         new CmdHandler<JsonValue,JsonValue>          (Echo));
            AddCommandHandlerAsync  (Std.Containers,   new CmdHandler<Empty,    Task<DbContainers>> (Containers));
            AddCommandHandler       (Std.Commands,     new CmdHandler<Empty,    DbCommands>         (Commands));
            AddCommandHandler       (Std.Schema,       new CmdHandler<Empty,    DbSchema>           (Schema));
            AddCommandHandlerAsync  (Std.Stats,        new CmdHandler<string,   Task<DbStats>>      (Stats));
            // --- host
            AddCommandHandler       (Std.HostDetails,  new CmdHandler<Empty,    HostDetails>        (Details));
            AddCommandHandlerAsync  (Std.HostCluster,  new CmdHandler<Empty,    Task<HostCluster>>  (Cluster));
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// private static string TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommand<TParam, TResult> (string name, Func<Command<TParam>, TResult> method) {
            AddCommandHandler (name, new CmdHandler<TParam, TResult> (method));
        }
        
        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// private static Task&lt;string&gt; TestCommand(Command&lt;int&gt; command) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandAsync<TParam, TResult> (string name, Func<Command<TParam>, Task<TResult>> method) {
            AddCommandHandlerAsync (name, new CmdHandler<TParam, Task<TResult>> (method));
        }
       
        /// <summary>
        /// Add all methods of the given <paramref name="handlerClass"/> using <see cref="Command{TParam}"/> as a
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
        protected void AddCommandHandlers<TClass>(TClass handlerClass, string commandPrefix) where TClass : class
        {
            var type                = handlerClass.GetType();
            var handlers            = TaskHandlerUtils.GetHandlers(type);
            if (handlers == null)
                return;
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            foreach (var handler in handlers) {
                // if (handler.name == "DbContainers") { int i = 1; }
                genericArgs[0]      = handler.valueType;
                genericArgs[1]      = handler.resultType;
                var genericTypeArgs = typeof(CmdHandler<,>).MakeGenericType(genericArgs);
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
        
        // ------------------------------ command handler methods ------------------------------
        private static JsonValue Echo (Command<JsonValue> command) {
            return command.JsonParam;
        }
        
        private static HostDetails Details (Command<Empty> command) {
            var hub     = command.Hub;
            var info    = hub.Info;
            var details = new HostDetails {
                version         = hub.Version,
                hostName        = hub.hostName,
                projectName     = info?.projectName,
                projectWebsite  = info?.projectWebsite,
                envName         = info?.envName,
                envColor        = info?.envColor
            };
            return details;
        }

        private static async Task<DbContainers> Containers (Command<Empty> command) {
            var database        = command.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbContainers;
        }
        
        private static DbCommands Commands (Command<Empty> command) {
            var database        = command.Database;  
            var dbCommands      = database.GetDbCommands();
            dbCommands.id       = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbCommands;
        }
        
        private static DbSchema Schema (Command<Empty> command) {
            var database        = command.Database;  
            var databaseName    = command.DatabaseName ?? EntityDatabase.MainDB;
            return ClusterStore.CreateCatalogSchema(database, databaseName);
        }
        
        private static async Task<DbStats> Stats (Command<string> command) {
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
        
        private static async Task<HostCluster> Cluster (Command<Empty> command) {
            var hub = command.Hub;
            return await ClusterStore.GetDbList(hub).ConfigureAwait(false);
        }
        
        // --- internal API ---
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command);
        }
        
        private void AddCommandHandler<TValue, TResult>(string name, CmdHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        private void AddCommandHandlerAsync<TValue, TResult>(string name, CmdHandler<TValue, Task<TResult>> handler) {
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