// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);

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
            // todo add handler via scanning TaskHandler
            AddCommandHandler       (StdCommand.DbEcho,        new CommandHandler<JsonValue, JsonValue>         (DbEcho));
            AddCommandHandler       (StdCommand.DbHubInfo,     new CommandHandler<Empty,     DbHubInfo>         (DbHubInfo));
            AddCommandHandlerAsync  (StdCommand.DbHubCluster,  new CommandHandler<Empty,     Task<DbHubCluster>>(DbHubCluster));
            AddCommandHandlerAsync  (StdCommand.DbContainers,  new CommandHandler<Empty,     Task<DbContainers>>(DbContainers));
            AddCommandHandler       (StdCommand.DbCommands,    new CommandHandler<Empty,     DbCommands>        (DbCommands));
            AddCommandHandler       (StdCommand.DbSchema,      new CommandHandler<Empty,     DbSchema>          (DbSchema));
        }
        
        private static JsonValue DbEcho (Command<JsonValue> command) {
            return command.JsonValue;
        }
        
        private static DbHubInfo DbHubInfo (Command<Empty> command) {
            var hub     = command.Hub;
            var info    = new DbHubInfo {
                version     = hub.Version,
                hostName    = hub.hostName,
                description = hub.description,
                website     = hub.website
            };
            return info;
        }

        private static async Task<DbContainers> DbContainers (Command<Empty> command) {
            var database        = command.Database;  
            var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
            dbContainers.id     = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbContainers;
        }
        
        private static DbCommands DbCommands (Command<Empty> command) {
            var database        = command.Database;  
            var dbCommands      = database.GetDbCommands();
            dbCommands.id       = command.DatabaseName ?? EntityDatabase.MainDB;
            return dbCommands;
        }
        
        private static DbSchema DbSchema (Command<Empty> command) {
            var database        = command.Database;  
            var databaseName    = command.DatabaseName ?? EntityDatabase.MainDB;
            return ClusterStore.CreateCatalogSchema(database, databaseName);
        }
        
        private static async Task<DbHubCluster> DbHubCluster (Command<Empty> command) {
            var hub = command.Hub;
            return await ClusterStore.GetDbList(hub).ConfigureAwait(false);
        }
        
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command); 
        }
        
        protected void AddCommandHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        protected void AddCommandHandlerAsync<TValue, TResult>(string name, CommandHandler<TValue, Task<TResult>> handler) {
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