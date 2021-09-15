// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL.Event;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    /// <summary>
    /// <see cref="EntityDatabase"/> is an abstraction for a specific database adapter / implementation e.g. a
    /// <see cref="MemoryDatabase"/> or a <see cref="FileDatabase"/>.
    /// An <see cref="EntityDatabase"/> contains multiple <see cref="EntityContainer"/>'s each representing
    /// a table / collection of a database. Each container is intended to store the records / entities of a specific type.
    /// E.g. one container for storing JSON objects representing 'articles' another one for storing 'orders'.
    /// <br/>
    /// An <see cref="EntityDatabase"/> instance is the single entry point used to handle all requests send by a client -
    /// e.g. an <see cref="Graph.EntityStore"/>. It handle these requests by its <see cref="ExecuteSync"/> method.
    /// A request is represented by a <see cref="SyncRequest"/> containing all database operations like create, read,
    /// upsert, delete and all messages / commands send by a client in the <see cref="SyncRequest.tasks"/> list.
    /// The <see cref="EntityDatabase"/> execute these tasks by its <see cref="taskHandler"/>.
    /// <br/>
    /// Instances of <see cref="EntityDatabase"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Graph.EntityStore"/> operating on the same <see cref="EntityDatabase"/> instance.
    /// To maintain thread safety <see cref="EntityDatabase"/> implementations must not have any mutable state.
    /// <br/>
    /// The <see cref="EntityDatabase"/> can be configured to support.
    /// <list type="bullet">
    ///     <item>A Pub-Sub implementation to send events for database changes or messages a client has subscribed.</item>
    ///     <item>Client / user authentication and task authorization. Each task is authorized individually.</item>
    ///     <item>Type / schema validation of JSON object written to its containers</item>
    /// </list>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityDatabase : IDisposable
    {
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        /// <summary>
        /// An optional <see cref="EventBroker"/> used to enable Pub-Sub. If enabled the database send
        /// events to a client for database changes and messages the client has subscribed.
        /// In case of remote database connections WebSockets are used to send Pub-Sub events to clients.   
        /// </summary>
        public              EventBroker                         eventBroker;
        /// <summary>
        /// The <see cref="TaskHandler"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// Custom task (request) handler can be added to the <see cref="taskHandler"/> or
        /// the <see cref="taskHandler"/> can be replaced by a custom implementation.
        /// </summary>
        public              TaskHandler                         taskHandler = new TaskHandler();
        /// <summary>
        /// An <see cref="Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="taskHandler"/>.
        /// </summary>
        public              Authenticator                       authenticator = new AuthenticateNone(new AuthorizeAllow());
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema                      schema;
        
        public              Func<string, string>                mapContainerName = MapContainerName; 
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        private static string MapContainerName (string name) {
            if (name == nameof (SequenceKeys))
                return "_SequenceKeys";
            if (name == nameof (Sequence))
                return "_Sequence";
            return name;
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
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.DB.Sync"/> which must to be fixed.</para> 
        /// </para>
        /// </summary>
        public virtual async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.clientId = syncRequest.clientId;
            await authenticator.Authenticate(syncRequest, messageContext).ConfigureAwait(false);
            
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
            SetContainerResults(response);
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
        
        /// Distribute <see cref="ContainerEntities.entityMap"/> to <see cref="ContainerEntities.entities"/>,
        /// <see cref="ContainerEntities.notFound"/> and <see cref="ContainerEntities.errors"/> to simplify and
        /// minimize response by removing redundancy.
        /// <see cref="EntityStore.GetContainerResults"/> remap these properties.
        protected virtual void SetContainerResults(SyncResponse response)
        {
            foreach (var resultPair in response.results) {
                var container       = resultPair.Value;
                var entityMap       = container.entityMap;
                var entities        = container.entities;
                List<JsonKey> notFound = null;
                var errors          = container.errors;
                container.errors    = null;
                entities.Capacity   = entityMap.Count;
                foreach (var entityPair in entityMap) {
                    EntityValue entity = entityPair.Value;
                    if (entity.Error != null) {
                        errors.Add(entityPair.Key, entity.Error);
                        continue;
                    }
                    var json = entity.Json;
                    if (json == null) {
                        if (notFound == null) {
                            notFound = new List<JsonKey>();
                        }
                        notFound.Add(entityPair.Key);
                        continue;
                    }
                    entities.Add(new JsonValue(json));
                }
                entityMap.Clear();
                if (notFound != null) {
                    container.notFound = notFound;
                }
                if (errors != null && errors.Count > 0) {
                    container.errors = errors;
                }
            }
        }
    }
}
