// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="FlioxHub"/> act as a Proxy between a <see cref="Client.FlioxClient"/> or a web browser and an <see cref="EntityDatabase"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="FlioxHub"/> features and utilization is available at
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub/Host/README.md#flioxhub">Host README.md</a>
    ///
    /// When creating a <see cref="FlioxHub"/> a <b>default <see cref="EntityDatabase"/></b> is assigned to the instance
    /// and all tasks requested by a client are applied to this <see cref="database"/>. <br/>
    /// A <see cref="FlioxHub"/> can be configured to support:
    /// <list type="bullet">
    ///   <item>
    ///     <b>Pub-Sub</b> to send events of database changes or messages to clients by assigning an
    ///     <see cref="Event.EventDispatcher"/> to <see cref="EventDispatcher"/>. <br/>
    ///     Note: A client need to subscribe events in order to receive them. 
    ///   </item>
    ///   <item>
    ///     <b>User authentication</b> and <b>task authorization</b>. Each task is authorized individually. <br/>
    ///     To enable this assign a <see cref="DB.UserAuth.UserAuthenticator"/> to <see cref="Authenticator"/>.
    ///   </item>
    ///   <item>
    ///     <b>Monitoring</b> of database access (requests) by adding a <see cref="DB.Monitor.MonitorDB"/> with
    ///     <see cref="AddExtensionDB"/>.
    ///   </item>
    /// </list>
    /// A <see cref="FlioxHub"/> instance handle <b>all</b> client requests by its <see cref="ExecuteRequestAsync"/> method. <br/>
    /// A request is represented by a <see cref="SyncRequest"/> and its <see cref="SyncRequest.tasks"/> are executed
    /// on the given <see cref="SyncRequest.database"/>. <br/>
    /// If database == null the default <see cref="database"/> of <see cref="FlioxHub"/> is used.
    /// <br/>
    /// The <see cref="SyncRequest.tasks"/> contains all database operations like create, read, upsert, delete
    /// and all messages / commands send by a client. <br/>
    /// The <see cref="FlioxHub"/> execute these tasks by the <see cref="EntityDatabase.service"/> of the
    /// specified <see cref="database"/>.<br/>
    /// <br/>
    /// Instances of <see cref="FlioxHub"/> are <b>thread-safe</b> enabling multiple clients e.g. <see cref="Client.FlioxClient"/>
    /// operating on the same <see cref="FlioxHub"/> instance. <br/>
    /// To maintain thread safety <see cref="FlioxHub"/> implementations must not have any mutable state.
    /// </remarks>
    public partial class FlioxHub : IDisposable, ILogSource
    {
    #region - members
        /// <summary> The default <see cref="database"/> assigned to the <see cref="FlioxHub"/> </summary>
        [Browse(Never)]
        public   readonly   EntityDatabase      database;                           // not null
        // ReSharper disable once UnusedMember.Local - show as property to list it within the first members in Debugger
        private             EntityDatabase      Database        => database;        // not null

        [Browse(Never)]
        public              IHubLogger          Logger          => sharedEnv.hubLogger;
        
        public              bool                PrettyCommandResults    { get; set; } = false;
        public              MonitorAccess       MonitorAccess           { get; set; }
        
        /// <summary>
        /// An optional <see cref="Event.EventDispatcher"/> used to enable Pub-Sub. <br/>
        /// If assigned the database send push events to clients for database changes and messages these clients have subscribed.
        /// </summary>
        /// <remarks>
        /// In case of remote database connections <b>WebSockets</b> are used to send Pub-Sub events to clients.   
        /// </remarks>
        public              EventDispatcher     EventDispatcher     { get; set; }
        
        protected internal virtual  bool        SupportPushEvents   => true;
        protected internal virtual  bool        IsRemoteHub         => false;
        
        /// <summary>
        /// An <see cref="Auth.Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="EntityDatabase.service"/>.
        /// </summary>
        public              Authenticator       Authenticator   { get => authenticator; set => authenticator = value ?? throw new ArgumentNullException(nameof(Authenticator)); }
        
        /// <summary>
        /// <see cref="ClientController"/> is used to create / add unique client ids to enable sending events to
        /// specific user clients.
        /// It also enables monitoring execution statistics of <see cref="ExecuteRequestAsync"/> 
        /// </summary>
        public              ClientController    ClientController{ get => clientController; set => clientController = value ?? throw new ArgumentNullException(nameof(ClientController)); }

        /// <summary>
        /// On server side the name can be used to identify a specific host if used within a cluster of servers.<br/>
        /// On client side any name can be assigned to simplify differentiation if using multiple <see cref="FlioxHub"/> instances.
        /// </summary>
        public              string              HostName        { get; init; } = "host";

        /// <summary>host <see cref="HostVersion"/> - available via command <b>std.Host</b></summary>
        public              string              HostVersion     { get; set; } = "1.0.0";
        
        public   static     string              FlioxVersion    => GetFlioxVersion();
        
        /// <summary>General descriptive Hub information - available via command <b>std.Host</b></summary>
        public              HubInfo             Info { get => info; set => info = value ?? throw new ArgumentNullException(nameof(Info)); }
        
        public   readonly   SharedEnv           sharedEnv;
        
        public   override   string              ToString()      => database.name;   // not null

        
        // --- private / internal fields & properties 
                        private  readonly   Dictionary<Type,object>     features        = new Dictionary<Type, object>();
                        internal readonly   HostStats                   hostStats       = new HostStats{ requestCount = new RequestCount{ db = new ShortString("*")} };
        [Browse(Never)] private             HubInfo                     info            = new HubInfo();
        [Browse(Never)] private             Authenticator               authenticator   = CreateDefaultAuthenticator();
        [Browse(Never)] private             ClientController            clientController= new IncrementClientController();
        
        /// <see cref="Authenticator"/> is mutable => create new instance per Hub 
        private static Authenticator CreateDefaultAuthenticator() {
            return new AuthenticateNone (TaskAuthorizer.Full, HubPermission.Full);
        }

        #endregion
        
    #region - initialize
        /// <summary>
        /// Construct a <see cref="FlioxHub"/> with the given default <paramref name="database"/>.
        /// </summary>
        public FlioxHub (EntityDatabase database, SharedEnv env = null)
        {
            sharedEnv           = env  ?? SharedEnv.Default;
            this.database       = database ?? throw new ArgumentNullException(nameof(database));
            extensionDbs        = new Dictionary<ShortString, EntityDatabase>(ShortString.Equality);
            Info.projectName    = this.database.Schema?.Name;
        }
        
        public virtual void Dispose() { }
        
        /// <summary>
        /// Utility methods calling <see cref="EntityDatabase.SetupDatabaseAsync"/> for the main and extension databases.<br/>
        /// <b>Note:</b> This method is intended for use during development. See docs at <see cref="EntityDatabase.SetupDatabaseAsync"/>
        /// </summary>
        public async Task SetupDatabasesAsync(Setup options = Setup.Default) {
            var databases = GetDatabases().Values;
            var tasks = new List<Task>();
            foreach (var db in databases) {
                var task = db.SetupDatabaseAsync(options);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        
        public TFeature TryGetFeature<TFeature>() where TFeature : new() {
            if (features.TryGetValue(typeof(TFeature), out var value)) {
                return (TFeature)value;
            }
            return default;
        }
        
        public TFeature GetFeature<TFeature>() where TFeature : new() {
            if (features.TryGetValue(typeof(TFeature), out var value)) {
                return (TFeature)value;
            }
            var feature                 = new TFeature();
            features[typeof(TFeature)]  = feature;
            return feature;
        }
        
        public void SetFeature<TFeature>(TFeature feature) where TFeature : new() {
            features[typeof(TFeature)] = feature;
        }

        private static string GetFlioxVersion() {
            var version     = typeof(FlioxHub).Assembly.GetName().Version;
            return version == null ? "-.-.-" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        
        /// <summary>
        /// Optimization for Unity to avoid heap allocations of <see cref="SyncResponse"/> instances and its dependencies
        /// </summary>
        internal virtual  ObjectPool<ReaderPool> GetResponseReaderPool() => null;
        
        #endregion
        
    #region - event receiver
        internal   virtual    void    AddEventReceiver      (in ShortString clientId, IEventReceiver eventReceiver) {}
        internal   virtual    void    RemoveEventReceiver   (in ShortString clientId) {}
        #endregion

    #region - sync request execution
        /// <summary>
        /// Before execution of a <see cref="SyncRequest"/> with <see cref="ExecuteRequestAsync"/> or <see cref="ExecuteRequest"/> 
        /// the <see cref="SyncRequest"/> must be initialized. <br/>
        /// If the request can be executed synchronously the method returns <see cref="ExecutionType.Sync"/><br/>
        /// Doing so avoids creation of a redundant <see cref="System.Threading.Tasks.Task"/> instance.  
        /// </summary>
        public virtual ExecutionType InitSyncRequest(SyncRequest syncRequest) {
            if (syncRequest.intern.executionType != None) {
                return syncRequest.intern.executionType;
            }
            var isSyncRequest       = authenticator.IsSynchronous(syncRequest);
            var db                  = database;
            if (!syncRequest.database.IsNull()) {
                if (!syncRequest.database.IsEqual(database.nameShort)) {
                    if (!extensionDbs.TryGetValue(syncRequest.database, out db)) {
                        syncRequest.intern.error = $"database not found: '{syncRequest.database.AsString()}'";
                        return syncRequest.intern.executionType = Error;
                    }
                }
            }
            syncRequest.intern.db   = db;
            var tasks               = syncRequest.tasks;
            if (tasks == null) {
                syncRequest.intern.error = "missing field: tasks (array)";
                return syncRequest.intern.executionType = Error;
            }
            var taskCount   = tasks.Count;
            var preExecute  = new PreExecute(db, sharedEnv, syncRequest.intern.executeSync);
            for (int index = 0; index < taskCount; index++)
            {
                var task = tasks[index];
                if (task == null) {
                    syncRequest.intern.error = $"tasks[{index}] == null";
                    return syncRequest.intern.executionType = Error;
                }
                task.intern.index   = index;
                // todo may validate tasks in PreExecute()
                var isSyncTask      = task.PreExecute(preExecute);
                isSyncRequest       = isSyncRequest && isSyncTask;
            }
            var executionType = isSyncRequest ? Sync : Async;
            syncRequest.intern.error = null;
            syncRequest.intern.executionType = executionType;
            return db.service.GetExecutionType(syncRequest);
        }

        public Task<ExecuteSyncResult> QueueRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            var queue   = syncRequest.intern.db.service.queue ?? throw new InvalidOperationException("DatabaseService initialized without a queue");
            var job     = new ServiceJob(this, syncRequest, syncContext);
            queue.EnqueueJob(job);
            return job.taskCompletionSource.Task;
        }
        
        /// <summary>
        /// Execute at the end of <see cref="ExecuteRequestAsync"/> and <see cref="ExecuteRequest"/>
        /// </summary>
        private void PostExecute(SyncRequest syncRequest, SyncResponse response, SyncContext syncContext) {
            var db = syncRequest.intern.db;
            var access = MonitorAccess; 
            if ((access & MonitorAccess.Host) != 0) {
                hostStats.Update(syncRequest);
            }
            if ((access & MonitorAccess.User) != 0) {
                UpdateRequestStats(db.nameShort, syncRequest, syncContext);
            }
            // - Note: Only relevant for Push messages when using a bidirectional protocol like WebSocket
            // As a client is required to use response.clientId it is set to null if given clientId was invalid.
            // So next request will create a new valid client id.
            response.clientId   = syncContext.clientIdValidation == ClientIdValidation.Invalid ? new ShortString() : syncContext.clientId;
            response.authError  = syncContext.authState.error;
            
            response.AssertResponse(syncRequest);
            
            db.service.PostExecuteTasks(syncContext);
            
            var dispatcher = EventDispatcher;
            if (dispatcher != null) {
                dispatcher.EnqueueSyncTasks(syncRequest, syncContext);
                if (dispatcher.dispatching == EventDispatching.Send) {
                    dispatcher.SendQueuedEvents(); // use only for testing
                }
            }
            // Clear intern references which are not used anymore at this point.
            syncRequest.intern = default;
        }
        
        private static TaskErrorResult TaskExceptionError (Exception e) {
            var exceptionName   = e.GetType().Name;
            var msg             = $"{exceptionName}: {e.Message}";
            var stack           = StackTraceUtils.GetStackTrace(e, false);
            return new TaskErrorResult (TaskErrorType.UnhandledException,msg, stack);
        }
        
        private static string GetLogMessage (string database, in ShortString user, int taskIndex, SyncRequestTask task) {
            var sb = new StringBuilder();
            sb.Append("database: "); sb.Append(database);
            sb.Append(", user: "); user.AppendTo(sb);
            sb.Append(", task["); sb.Append(taskIndex); sb.Append("]: "); sb.Append(task.TaskType);
            sb.Append(" ("); sb.Append(task.TaskName); sb.Append(')');
            return sb.ToString();
        }
        
        private void UpdateRequestStats(in ShortString database, SyncRequest syncRequest, SyncContext syncContext) {
            var user = syncContext.User;
            ClusterUtils.UpdateCountsMap(user.requestCounts, database, syncRequest);
            ref var clientId = ref syncContext.clientId;
            if (clientId.IsNull())
                return;
            if (clientController.clients.TryGetValue(clientId, out UserClient client)) {
                ClusterUtils.UpdateCountsMap(client.requestCounts, database, syncRequest);
            }
        }
        #endregion

    #region - extension databases
        [Browse(Never)]
        private readonly  Dictionary<ShortString, EntityDatabase>   extensionDbs;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private           IReadOnlyCollection<EntityDatabase>       ExtensionDbs => extensionDbs.Values;
        
        /// <summary>
        /// Add an <paramref name="extensionDB"/> to the Hub. The extension database is identified by its
        /// <see cref="EntityDatabase.nameShort"/>
        /// </summary>
        public void AddExtensionDB(EntityDatabase extensionDB) {
            extensionDbs.Add(extensionDB.nameShort, extensionDB);
            var msg = $"add extension db: {extensionDB.nameShort} ({extensionDB.StorageType})";
            Logger.Log(HubLog.Info, msg);
        }
        
        public bool TryGetDatabase(string name, out EntityDatabase value) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (name == database.name) {
                value = database;
                return true;
            }
            var databaseName = new ShortString(name);
            return extensionDbs.TryGetValue(databaseName, out value);
        }
        
        internal EntityDatabase FindDatabase(ShortString name) {
            if (name.IsNull() || name.IsEqual(database.nameShort)) {
                return database;
            }
            extensionDbs.TryGetValue(name, out var result);
            return result;
        }

        public Dictionary<string, EntityDatabase> GetDatabases() {
            var result = new Dictionary<string, EntityDatabase> (extensionDbs.Count + 1) {
                { database.name, database }
            };
            foreach (var pair in extensionDbs) {
                var extensionDB = pair.Value; 
                result.Add(extensionDB.name, extensionDB);
            }
            return result;
        }
        

        #endregion
    }
    
    [Flags]
    public enum MonitorAccess
    {
        User    = 1,
        Host    = 2,
        All     = User | Host
    }
}
