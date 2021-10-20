// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Host.Stats;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    public class DbOpt {
        /// <see cref="DatabaseHub.hostName"/>
        public  readonly    string                  hostName;
        /// <see cref="DatabaseHub.customContainerName"/>
        public  readonly    CustomContainerName     customContainerName;
        
        public DbOpt(string hostName = null, CustomContainerName customContainerName = null) {
            this.hostName               = hostName              ?? "host";
            this.customContainerName    = customContainerName   ?? (name => name);
        }
        
        internal static readonly DbOpt Default = new DbOpt();
    }
    
    public delegate string CustomContainerName(string name);

    /// <summary>
    /// <see cref="DatabaseHub"/> is an abstraction for a specific database adapter / implementation e.g. a
    /// <see cref="MemoryDatabase"/> or a <see cref="FileDatabase"/>.
    /// An <see cref="DatabaseHub"/> contains multiple <see cref="EntityContainer"/>'s each representing
    /// a table / collection of a database. Each container is intended to store the records / entities of a specific type.
    /// E.g. one container for storing JSON objects representing 'articles' another one for storing 'orders'.
    /// <br/>
    /// An <see cref="DatabaseHub"/> instance is the single entry point used to handle all requests send by a client -
    /// e.g. an <see cref="Client.FlioxClient"/>. It handle these requests by its <see cref="ExecuteSync"/> method.
    /// A request is represented by a <see cref="SyncRequest"/> containing all database operations like create, read,
    /// upsert, delete and all messages / commands send by a client in the <see cref="SyncRequest.tasks"/> list.
    /// The <see cref="DatabaseHub"/> execute these tasks by its <see cref="taskHandler"/>.
    /// <br/>
    /// Instances of <see cref="DatabaseHub"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Client.FlioxClient"/> operating on the same <see cref="DatabaseHub"/> instance.
    /// To maintain thread safety <see cref="DatabaseHub"/> implementations must not have any mutable state.
    /// <br/>
    /// The <see cref="DatabaseHub"/> can be configured to support.
    /// <list type="bullet">
    ///   <item>
    ///     A Pub-Sub implementation to send events of database changes or messages to clients by assigning an
    ///     <see cref="Event.EventBroker"/> to <see cref="EventBroker"/>.
    ///     Note: A client need to subscribe events in order to receive them. 
    ///   </item>
    ///   <item>
    ///     User authentication and task authorization. Each task is authorized individually. To enable this assign 
    ///     a <see cref="UserAuth.UserAuthenticator"/> to <see cref="Authenticator"/>.
    ///   </item>
    ///   <item>
    ///     Type / schema validation of JSON entities written (create, update and patch) to its containers by assigning
    ///     a <see cref="DatabaseSchema"/> to <see cref="Schema"/>.
    ///   </item>
    ///   <item>
    ///     Monitoring of database access (requests) by adding a <see cref="Monitor.MonitorDatabase"/> with
    ///     <see cref="AddExtensionDB(DatabaseHub)"/>.
    ///   </item>
    /// </list>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class DatabaseHub : IDisposable
    {
        /// <summary> map of of containers identified by their container name </summary>
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        
        /// <summary>
        /// An optional <see cref="Event.EventBroker"/> used to enable Pub-Sub. If enabled the database send
        /// events to a client for database changes and messages the client has subscribed.
        /// In case of remote database connections WebSockets are used to send Pub-Sub events to clients.   
        /// </summary>
        public              EventBroker         EventBroker     { get; set; }
        
        /// <summary>
        /// The <see cref="TaskHandler"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// Custom task (request) handler can be added to the <see cref="taskHandler"/> or
        /// the <see cref="taskHandler"/> can be replaced by a custom implementation.
        /// </summary>
        public readonly     TaskHandler         taskHandler;
        
        /// <summary>
        /// An <see cref="Auth.Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="taskHandler"/>.
        /// </summary>
        public              Authenticator       Authenticator   { get => authenticator; set => authenticator = NotNull(value, nameof(Authenticator)); }
        
        /// <summary>
        /// <see cref="ClientController"/> is used to create / add unique client ids to enable sending events to
        /// specific user clients.
        /// It also enables monitoring execution statistics of <see cref="DatabaseHub.ExecuteSync"/> 
        /// </summary>
        public              ClientController    ClientController{ get => clientController; set => clientController = NotNull(value, nameof(ClientController)); }
        
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; set; }
        
        /// <summary>
        /// A host name that is assigned to a default database.
        /// Its only purpose is to use it as id in <see cref="Monitor.HostInfo.id"/>.
        /// </summary>
        /// 
        public  readonly    string              hostName;
        
        /// <summary>
        /// A mapping function used to assign a custom container name.
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="DatabaseHub"/> it enables uniform mapping across different
        /// <see cref="DatabaseHub"/> implementations.
        /// </summary>
        public readonly     CustomContainerName customContainerName;
        
        internal readonly   HostStats           hostStats = new HostStats();
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private             Authenticator       authenticator       = new AuthenticateNone(new AuthorizeAllow());
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private             ClientController    clientController    = new IncrementClientController();

        public override     string              ToString() => extensionName != null ? $"'{extensionName}'" : "";
        
        /// <summary> Construct a default database </summary>
        protected DatabaseHub (DbOpt opt, TaskHandler taskHandler = null) {
            hostName            = (opt ?? DbOpt.Default).hostName;
            customContainerName = (opt ?? DbOpt.Default).customContainerName;
            this.taskHandler    = taskHandler ?? new TaskHandler();
        }
        
        /// <summary> Construct an extension database </summary>
        protected DatabaseHub (
            DatabaseHub  extensionBase,
            string          extensionName,
            DbOpt           opt,
            TaskHandler     taskHandler = null
        ) : this(opt, taskHandler) {
            hostName = null; // extension database must not have a hostName to avoid confusion
            this.extensionBase  = extensionBase ?? throw new ArgumentNullException(nameof(extensionBase));
            this.extensionName  = extensionName ?? throw new ArgumentNullException(nameof(extensionName));
        }
        
        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        private static T NotNull<T> (T value, string name) where T : class => value ?? throw new NullReferenceException(name);
        
        public abstract EntityContainer CreateContainer     (string name, DatabaseHub database);
        public virtual  void            AddEventTarget      (in JsonKey clientId, IEventTarget eventTarget) {}
        public virtual  void            RemoveEventTarget   (in JsonKey clientId) {}

        internal void AddContainer(EntityContainer container) {
            containers.Add(container.name, container);
        }
        
        public bool TryGetContainer(string name, out EntityContainer container) {
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
            DatabaseHub db = this;
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
            hostStats.Update(syncRequest);
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
            RequestCount.UpdateCounts(user.requestCounts, database, syncRequest);
            ref var clientId = ref messageContext.clientId;
            if (clientId.IsNull())
                return;
            if (clientController.clients.TryGetValue(clientId, out UserClient client)) {
                RequestCount.UpdateCounts(client.requestCounts, database, syncRequest);
            }
        }

        // --------------------------------- extension databases ---------------------------------
        internal readonly   string                              extensionName;
        internal readonly   DatabaseHub                      extensionBase;
        private  readonly   Dictionary<string, DatabaseHub>  extensionDbs = new Dictionary<string, DatabaseHub>();
        
        public void AddExtensionDB(DatabaseHub extensionDB) {
            extensionDbs.Add(extensionDB.extensionName, extensionDB);
        }

        public DatabaseHub AddExtensionDB (string extensionName, DbOpt opt = null) {
            var extensionDB = new ExtensionDatabase (this, extensionName, opt);
            extensionDbs.Add(extensionName, extensionDB);
            return extensionDB;
        }
    }
}
