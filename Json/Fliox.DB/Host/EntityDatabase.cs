// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    /// <summary>
    /// <see cref="EntityDatabase"/> is an abstraction for a specific database adapter / implementation e.g. a
    /// <see cref="MemoryDatabase"/> or a <see cref="FileDatabase"/>.
    /// An <see cref="EntityDatabase"/> contains multiple <see cref="EntityContainer"/>'s each representing
    /// a table / collection of a database. Each container is intended to store the records / entities of a specific type.
    /// E.g. one container for storing JSON objects representing 'articles' another one for storing 'orders'.
    /// <br/>
    /// An <see cref="EntityDatabase"/> instance is the single entry point used to handle all requests send by a client -
    /// e.g. an <see cref="Client.EntityStore"/>. It handle these requests by its <see cref="ExecuteSync"/> method.
    /// A request is represented by a <see cref="SyncRequest"/> containing all database operations like create, read,
    /// upsert, delete and all messages / commands send by a client in the <see cref="SyncRequest.tasks"/> list.
    /// The <see cref="EntityDatabase"/> execute these tasks by its <see cref="TaskHandler"/>.
    /// <br/>
    /// Instances of <see cref="EntityDatabase"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Client.EntityStore"/> operating on the same <see cref="EntityDatabase"/> instance.
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
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        /// <summary>
        /// An optional <see cref="Event.EventBroker"/> used to enable Pub-Sub. If enabled the database send
        /// events to a client for database changes and messages the client has subscribed.
        /// In case of remote database connections WebSockets are used to send Pub-Sub events to clients.   
        /// </summary>
        public              EventBroker         EventBroker     { get; set; }
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        /// <summary>
        /// The <see cref="Host.TaskHandler"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// Custom task (request) handler can be added to the <see cref="TaskHandler"/> or
        /// the <see cref="TaskHandler"/> can be replaced by a custom implementation.
        /// </summary>
        public              TaskHandler         TaskHandler     { get => taskHandler; set => taskHandler = NotNull(value, nameof(TaskHandler)); }
        /// <summary>
        /// An <see cref="Auth.Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="TaskHandler"/>.
        /// </summary>
        public              Authenticator       Authenticator   { get => authenticator; set => authenticator = NotNull(value, nameof(Authenticator)); }
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        /// <summary>
        /// <see cref="ClientController"/> is used to create / add unique client ids to enable sending events to
        /// specific user clients.
        /// It also enables monitoring execution statistics of <see cref="EntityDatabase.ExecuteSync"/> 
        /// </summary>
        public              ClientController    ClientController{ get => clientController; set => clientController = NotNull(value, nameof(ClientController)); }
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; set; }
        /// <summary>
        /// A mapping function used to assign a custom container name.
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="EntityDatabase"/> it enables uniform mapping across different
        /// <see cref="EntityDatabase"/> implementations.
        /// </summary>
        public              CustomContainerName CustomContainerName { get => customContainerName; set => customContainerName = NotNull(value, nameof(CustomContainerName)); }
        
        private static T    NotNull<T> (T value, string name) where T : class => value ?? throw new NullReferenceException(name);
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private   TaskHandler         taskHandler         = new TaskHandler();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private   Authenticator       authenticator       = new AuthenticateNone(new AuthorizeAllow());
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private   ClientController    clientController    = new IncrementClientController();
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private   CustomContainerName customContainerName = name => name;

        public override     string                              ToString() => extensionName != null ? $"'{extensionName}'" : "";
        
        // ReSharper disable once EmptyConstructor - keep for code navigation
        protected EntityDatabase () { }
        protected EntityDatabase (EntityDatabase extensionBase, string extensionName) {
            this.extensionBase = extensionBase ?? throw new ArgumentException(nameof(extensionBase));
            this.extensionName = extensionName ?? throw new ArgumentException(nameof(extensionName));
        }
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        public virtual void AddEventTarget     (in JsonKey clientId, IEventTarget eventTarget) {}
        public virtual void RemoveEventTarget  (in JsonKey clientId) {}

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
        ///   <see cref="ExecuteSync"/> catches exceptions thrown by a <see cref="SyncRequestTask"/> but 
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
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.DB.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </summary>
        public virtual async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            if (messageContext.authState.AuthExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            messageContext.clientId = syncRequest.clientId;
            
            await authenticator.Authenticate(syncRequest, messageContext).ConfigureAwait(false);
            messageContext.clientIdValidation = authenticator.ValidateClientId(clientController, messageContext);

            var database = syncRequest.database;
            EntityDatabase db = this;
            if (database != null) {
                if (!extensionDbs.TryGetValue(database, out db))
                    return new MsgResponse<SyncResponse>($"database not found: '{syncRequest.database}'");
                await db.ExecuteSyncPrepare(syncRequest, messageContext).ConfigureAwait(false);
            }
            if (database != db.extensionName)
                throw new InvalidOperationException($"Unexpected ExtensionName. expect: {database}, was: {db.extensionName}");
                    
            var requestTasks = syncRequest.tasks;
            if (requestTasks == null) {
                return new MsgResponse<SyncResponse> ("missing field: tasks (array)");
            }
            var tasks       = new List<SyncTaskResult>(requestTasks.Count);
            var resultMap   = new Dictionary<string, ContainerEntities>();
            var response    = new SyncResponse { tasks = tasks, resultMap = resultMap, database = database };
            
            for (int index = 0; index < requestTasks.Count; index++) {
                var task = requestTasks[index];
                if (task == null) {
                    tasks.Add(SyncRequestTask.InvalidTask($"element must not be null. tasks[{index}]"));
                    continue;
                }
                task.index = index;
                try {
                    var result = await db.taskHandler.ExecuteTask(task, db, response, messageContext).ConfigureAwait(false);
                    tasks.Add(result);
                } catch (Exception e) {
                    tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                }
            }
            UpdateRequestStats(database, syncRequest, messageContext);

            // - Note: Only relevant for Push messages when using a bidirectional protocol like WebSocket
            // As a client is required to use response.clientId it is set to null if given clientId was invalid.
            // So next request will create a new valid client id.
            response.clientId = messageContext.clientIdValidation == ClientIdValidation.Invalid ? new JsonKey() : messageContext.clientId;
            
            response.AssertResponse(syncRequest);
            
            var broker = EventBroker;
            if (broker != null) {
                broker.EnqueueSyncTasks(syncRequest, messageContext);
                if (!broker.background) {
                    await broker.SendQueuedEvents().ConfigureAwait(false); // use only for testing
                }
            }
            return new MsgResponse<SyncResponse>(response);
        }
        
        private static TaskErrorResult TaskExceptionError (Exception e) {
            var exceptionName   = e.GetType().Name;
            var msg             = $"{exceptionName}: {e.Message}";
            var stack           = e.StackTrace;
            return new TaskErrorResult{ type = TaskErrorResultType.UnhandledException, message = msg, stacktrace  = stack };
        }

        protected virtual Task ExecuteSyncPrepare (SyncRequest syncRequest, MessageContext messageContext) {
            return Task.CompletedTask;
        }
        
        private void UpdateRequestStats(string database, SyncRequest syncRequest, MessageContext messageContext) {
            if (database == null) database = "default";
            var user = messageContext.authState.User;
            RequestStats.Update(user.stats, database, syncRequest);
            ref var clientId = ref messageContext.clientId;
            if (clientId.IsNull())
                return;
            if (clientController.clients.TryGetValue(clientId, out UserClient client)) {
                RequestStats.Update(client.stats, database, syncRequest);
            }
        }

        // --------------------------------- extension databases ---------------------------------
        internal readonly   string                              extensionName;
        internal readonly   EntityDatabase                      extensionBase;
        private  readonly   Dictionary<string, EntityDatabase>  extensionDbs = new Dictionary<string, EntityDatabase>();
        
        public void AddExtensionDB(EntityDatabase extensionDB) {
            extensionDbs.Add(extensionDB.extensionName, extensionDB);
        }

        public EntityDatabase AddExtensionDB (string extensionName) {
            var extensionDB = new ExtensionDatabase (this, extensionName);
            extensionDbs.Add(extensionName, extensionDB);
            return extensionDB;
        }
    }
    
    public delegate string CustomContainerName(string name);
}
