// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);

    public class TaskHandler
    {
        private readonly Dictionary<string, CommandCallback> commands = new Dictionary<string, CommandCallback>();
        
        public TaskHandler () {
            AddCommandHandler(StdMessage.Echo, new CommandHandler<JsonValue, JsonValue>(Echo));
        }
        
        private static JsonValue Echo (Command<JsonValue> command) {
            return command.JsonValue;
        }
        
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command); 
        }
        
        public void AddCommandHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandler<TValue, TResult>(CommandHandler<TValue, TResult> handler) {
            var name = typeof(TValue).Name;
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandlerAsync<TValue, TResult>(string name, CommandHandler<TValue, Task<TResult>> handler) {
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandlerAsync<TValue, TResult>(CommandHandler<TValue, Task<TResult>> handler) {
            var name = typeof(TValue).Name;
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }

        private static bool AuthorizeTask(SyncRequestTask task, MessageContext messageContext, out SyncTaskResult error) {
            var authorizer = messageContext.authState.authorizer;
            if (authorizer.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var message = "not authorized";
            var authError = messageContext.authState.error; 
            if (authError != null) {
                message = $"{message}. {authError}";
            }
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return error;
            }
            var result = await task.Execute(database, response, messageContext).ConfigureAwait(false);
            return result;
        }
    }
}