// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class TaskHandler
    {
        private readonly Dictionary<string, CommandCallback> commands = new Dictionary<string, CommandCallback>();
        
        public void AddMessageHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        private static bool AuthorizeTask(DatabaseTask task, MessageContext messageContext, out TaskResult error) {
            if (messageContext.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var message = "not authorized";
            var authError = messageContext.authState.Error; 
            if (authError != null) {
                message = $"{message} ({authError})";
            }
            error = DatabaseTask.PermissionDenied(message);
            return false;
        }
        
        public virtual Task<TaskResult> ExecuteTask (DatabaseTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (task is SendMessage message) {
                var commandName = message.name;
                if (commands.TryGetValue(commandName, out CommandCallback callback)) {
                    using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                        var mapper      = pooledMapper.instance;
                        var jsonResult  = callback.InvokeCallback(mapper, commandName, message.value);
                        var value       = new JsonValue { json = jsonResult };
                        var sendResult  = new SendMessageResult { result = value };
                        return Task.FromResult<TaskResult>(sendResult);
                    }
                }
            }
            
            if (!AuthorizeTask(task, messageContext, out var error)) {
                return Task.FromResult(error);
            }
            Task<TaskResult> result = task.Execute(database, response, messageContext);
            return result;
        }
    }
}