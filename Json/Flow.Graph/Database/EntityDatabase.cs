// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Auth;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityDatabase : IDisposable
    {
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        public              EventBroker                         eventBroker;
        public              TaskHandler                         taskHandler = new TaskHandler();
        public              Authenticator                       authenticator = new AuthenticateNone(new AuthorizeAllow());
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        public virtual void AddEventTarget     (string clientId, IEventTarget eventTarget) {}
        public virtual void RemoveEventTarget  (string clientId) {}

        internal void AddContainer(EntityContainer container)
        {
            containers.Add(container.name, container);
        }
        
        public bool TryGetContainer(string name, out EntityContainer container)
        {
            return containers.TryGetValue(name, out container);
        }

        public EntityContainer GetOrCreateContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        /// <summary>
        /// Execute all <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
        /// <para>
        ///   <see cref="ExecuteSync"/> catches exceptions thrown by a <see cref="DatabaseTask"/> but 
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
        ///          A proper error handling requires to set a meaningful <see cref="CommandError"/> to
        ///          <see cref="ICommandResult.Error"/></para>
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Flow.Sync"/> which must to be fixed.</para> 
        /// </para>
        /// </summary>
        public virtual async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.clientId = syncRequest.clientId;
            await authenticator.Authenticate(syncRequest, messageContext);
            
            var requestTasks = syncRequest.tasks;
            if (requestTasks == null)
                return new SyncResponse{error = new ErrorResponse{message = "missing field: tasks (array)"}};
            var tasks = new List<TaskResult>(requestTasks.Count);
            var response = new SyncResponse {
                tasks   = tasks,
                results = new Dictionary<string, ContainerEntities>()
            };
            int index = -1;
            foreach (var task in requestTasks) {
                index++;
                if (task == null) {
                    var taskResult = new TaskErrorResult{
                        type        = TaskErrorResultType.InvalidTask,
                        message     = $"element must not be null. tasks[{index}]"
                    };
                    tasks.Add(taskResult);
                    continue;
                }
                task.index = index;
                    
                try {
                    TaskResult result = await taskHandler.ExecuteTask(task, this, response, messageContext).ConfigureAwait(false);
                    tasks.Add(result);
                }
                catch (Exception e) {
                    // Note!
                    // Should not happen - see documentation of this method.
                    var exceptionName = e.GetType().Name;
                    var message = $"{exceptionName}: {e.Message}";
                    var stacktrace = e.StackTrace;
                    var result = new TaskErrorResult{
                        type        = TaskErrorResultType.UnhandledException,
                        message     = message,
                        stacktrace  = stacktrace
                    };
                    tasks.Add(result);
                }
            }
            response.AssertResponse(syncRequest);
            
            var broker = eventBroker;
            if (broker != null) {
                broker.EnqueueSyncTasks(syncRequest, messageContext);
                if (!broker.background) {
                    await broker.SendQueuedEvents().ConfigureAwait(false); // use only for testing
                }
            }
            return response;
        }
    }
}
