// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

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
    ///     <b>Monitoring</b> of database access (requests) by adding a <see cref="MonitorDB"/> with
    ///     <see cref="AddExtensionDB"/>.
    ///   </item>
    /// </list>
    /// A <see cref="FlioxHub"/> instance handle <b>all</b> client requests by its <see cref="ExecuteSync"/> method. <br/>
    /// A request is represented by a <see cref="SyncRequest"/> and its <see cref="SyncRequest.tasks"/> are executed
    /// on the given <see cref="SyncRequest.database"/>. <br/>
    /// If database == null the default <see cref="database"/> of <see cref="FlioxHub"/> is used.
    /// <br/>
    /// The <see cref="SyncRequest.tasks"/> contains all database operations like create, read, upsert, delete
    /// and all messages / commands send by a client. <br/>
    /// The <see cref="FlioxHub"/> execute these tasks by the <see cref="EntityDatabase.handler"/> of the
    /// specified <see cref="database"/>.<br/>
    /// <br/>
    /// Instances of <see cref="FlioxHub"/> are <b>thread-safe</b> enabling multiple clients e.g. <see cref="Client.FlioxClient"/>
    /// operating on the same <see cref="FlioxHub"/> instance. <br/>
    /// To maintain thread safety <see cref="FlioxHub"/> implementations must not have any mutable state.
    /// </remarks>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FlioxHub : IDisposable, ILogSource
    {
    #region - members
        /// <summary> The default <see cref="database"/> assigned to the <see cref="FlioxHub"/> </summary>
        [DebuggerBrowsable(Never)]
        public   readonly   EntityDatabase      database;       // not null
        // ReSharper disable once UnusedMember.Local - show as property to list it within the first members in Debugger
        private             EntityDatabase      Database        => database;
        /// <summary> name of the default <see cref="database"/> assigned to the <see cref="FlioxHub"/> </summary>
        public              string              DatabaseName    => database.name; // not null
        public   override   string              ToString()      => database.name;
        
        [DebuggerBrowsable(Never)]
        public              IHubLogger          Logger          => sharedEnv.hubLogger;
        
        /// <summary>
        /// An optional <see cref="Event.EventDispatcher"/> used to enable Pub-Sub. <br/>
        /// If assigned the database send push events to clients for database changes and messages these clients have subscribed.
        /// </summary>
        /// <remarks>
        /// In case of remote database connections <b>WebSockets</b> are used to send Pub-Sub events to clients.   
        /// </remarks>
        public              EventDispatcher     EventDispatcher     { get; set; }
        
        public virtual      bool                SupportPushEvents   => true;
        
        /// <summary>
        /// An <see cref="Auth.Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="EntityDatabase.handler"/>.
        /// </summary>
        public              Authenticator       Authenticator   { get => authenticator; set => authenticator = value ?? throw new ArgumentNullException(nameof(Authenticator)); }
        
        /// <summary>
        /// <see cref="ClientController"/> is used to create / add unique client ids to enable sending events to
        /// specific user clients.
        /// It also enables monitoring execution statistics of <see cref="FlioxHub.ExecuteSync"/> 
        /// </summary>
        public              ClientController    ClientController{ get => clientController; set => clientController = value ?? throw new ArgumentNullException(nameof(ClientController)); }
        
        /// <summary>
        /// A host name that is assigned to a default database.
        /// Its only purpose is to use it as id in <see cref="HostHits.id"/>.
        /// </summary>
        public   readonly   string                  hostName;
        
        /// <summary>host <see cref="HostVersion"/> - available via command <b>std.Host</b></summary>
        public              string                  HostVersion    { get; set; } = "1.0.0";
        
        public   static     string                  FlioxVersion    => GetFlioxVersion();
        
        /// <summary>General Hub information - available via command <b>std.Host</b></summary>
        public              HubInfo                 Info { get => info; set => info = value ?? throw new ArgumentNullException(nameof(Info)); }
        
        /// <summary><see cref="Routes"/> exposed by the Host - available via command <b>std.Host</b> </summary>
        public              IReadOnlyList<string>   Routes => routes;
        
        public   readonly   SharedEnv               sharedEnv;
        
        internal readonly   HostStats               hostStats   = new HostStats{ requestCount = new RequestCount{ db = "*"} };
        [DebuggerBrowsable(Never)]
        internal readonly   List<string>            routes      = new List<string>();

        [DebuggerBrowsable(Never)]  private HubInfo             info                = new HubInfo();
        [DebuggerBrowsable(Never)]  private Authenticator       authenticator       = CreateDefaultAuthenticator();
        [DebuggerBrowsable(Never)]  private ClientController    clientController    = new IncrementClientController();
        
        /// <see cref="Authenticator"/> is mutable => create new instance per Hub 
        private static Authenticator CreateDefaultAuthenticator() {
            return new AuthenticateNone { AnonymousTaskAuthorizer = TaskAuthorizer.Full, AnonymousHubPermission = HubPermission.Full };
        }

        #endregion
        
    #region - initialize
        /// <summary>
        /// Construct a <see cref="FlioxHub"/> with the given default <paramref name="database"/>.
        /// </summary>
        public FlioxHub (EntityDatabase database, SharedEnv env = null, string hostName = null) {
            sharedEnv       = env  ?? SharedEnv.Default;
            this.database   = database ?? throw new ArgumentNullException(nameof(database));
            this.hostName   = hostName ?? "host";
        }
        
        public virtual void Dispose() { }  // todo - remove
        
        private static string GetFlioxVersion() {
            var version     = typeof(FlioxHub).Assembly.GetName().Version;
            return version == null ? "-.-.-" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        
        #endregion
        
    #region - event receiver
        public   virtual    void    AddEventReceiver      (in JsonKey clientId, IEventReceiver eventReceiver) {}
        public   virtual    void    RemoveEventReceiver   (in JsonKey clientId) {}
        #endregion

    #region - sync request execution
        /// <summary>
        /// Execute all <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/> send by client.
        /// </summary>
        /// <remarks>
        /// All requests to a <see cref="FlioxHub"/> are handled by this method.
        /// By design this is the 'front door' all requests have to pass to get processed.
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
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.Hub.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </remarks>
        public virtual async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            syncContext.hub = this;
            var syncDbName  = syncRequest.database;             // is nullable
            var hubDbName   = syncContext.hub.DatabaseName;     // not null
            var dbName      = syncDbName ?? hubDbName;          // not null
            syncContext.DatabaseName = dbName;
            if (syncContext.authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            syncContext.clientId = syncRequest.clientId;
            
            await authenticator.Authenticate(syncRequest, syncContext).ConfigureAwait(false);
            syncContext.clientIdValidation = authenticator.ValidateClientId(clientController, syncContext);
            
            var requestTasks = syncRequest.tasks;
            if (requestTasks == null) {
                return new ExecuteSyncResult ("missing field: tasks (array)", ErrorResponseType.BadRequest);
            }
            for (int index = 0; index < requestTasks.Count; index++) {
                var task = requestTasks[index];
                if (task != null) {
                    task.index = index;
                    continue;
                }
                return new ExecuteSyncResult($"tasks[{index}] == null", ErrorResponseType.BadRequest);
            }
            EntityDatabase db = database;
            if (dbName != hubDbName) {
                if (!extensionDbs.TryGetValue(dbName, out db))
                    return new ExecuteSyncResult($"database not found: '{syncRequest.database}'", ErrorResponseType.BadRequest);
                await db.ExecuteSyncPrepare(syncRequest, syncContext).ConfigureAwait(false);
            }
            var tasks       = new List<SyncTaskResult>(requestTasks.Count);
            var resultMap   = new Dictionary<string, ContainerEntities>();
            var response    = new SyncResponse { tasks = tasks, resultMap = resultMap, database = syncDbName };
            var taskHandler = db.handler;
            
            // ------------------------ loop through all given tasks and execute them ------------------------
            for (int index = 0; index < requestTasks.Count; index++) {
                var task = requestTasks[index];
                try {
                    var result = await taskHandler.ExecuteTask(task, db, response, syncContext).ConfigureAwait(false);
                    tasks.Add(result);
                } catch (Exception e) {
                    tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                    var message = GetLogMessage(dbName, syncRequest.userId, index, task);
                    Logger.Log(HubLog.Error, message, e);
                }
            }
            hostStats.Update(syncRequest);
            UpdateRequestStats(dbName, syncRequest, syncContext);

            // - Note: Only relevant for Push messages when using a bidirectional protocol like WebSocket
            // As a client is required to use response.clientId it is set to null if given clientId was invalid.
            // So next request will create a new valid client id.
            response.clientId = syncContext.clientIdValidation == ClientIdValidation.Invalid ? new JsonKey() : syncContext.clientId;
            
            response.AssertResponse(syncRequest);
            
            var dispatcher = EventDispatcher;
            if (dispatcher != null) {
                dispatcher.EnqueueSyncTasks(syncRequest, syncContext);
                if (!dispatcher.background) {
                    await dispatcher.SendQueuedEvents().ConfigureAwait(false); // use only for testing
                }
            }
            return new ExecuteSyncResult(response);
        }
        
        private static TaskErrorResult TaskExceptionError (Exception e) {
            var exceptionName   = e.GetType().Name;
            var msg             = $"{exceptionName}: {e.Message}";
            var stack           = StackTraceUtils.GetStackTrace(e, false);
            return new TaskErrorResult (TaskErrorResultType.UnhandledException,msg, stack);
        }
        
        private static string GetLogMessage (string database, in JsonKey user, int taskIndex, SyncRequestTask task) {
            var sb = new StringBuilder();
            sb.Append("database: "); sb.Append(database);
            sb.Append(", user: "); user.AppendTo(sb);
            sb.Append(", task["); sb.Append(taskIndex); sb.Append("]: "); sb.Append(task.TaskType);
            sb.Append(" ("); sb.Append(task.TaskName); sb.Append(')');
            return sb.ToString();
        }
        
        private void UpdateRequestStats(string database, SyncRequest syncRequest, SyncContext syncContext) {
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
        [DebuggerBrowsable(Never)]
        private readonly  Dictionary<string, EntityDatabase>    extensionDbs = new Dictionary<string, EntityDatabase>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private           IReadOnlyCollection<EntityDatabase>   ExtensionDbs => extensionDbs.Values;
        
        /// <summary>
        /// Add an <paramref name="extensionDB"/> to the Hub. The extension database is identified by its
        /// <see cref="EntityDatabase.name"/>
        /// </summary>
        public void AddExtensionDB(EntityDatabase extensionDB) {
            extensionDbs.Add(extensionDB.name, extensionDB);
            var msg = $"add extension db: {extensionDB.name} ({extensionDB.StorageType})";
            Logger.Log(HubLog.Info, msg);
        }
        
        public bool TryGetDatabase(string name, out EntityDatabase value) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (name == database.name) {
                value = database;
                return true;
            }
            return extensionDbs.TryGetValue(name, out value);
        }
        
        public EntityDatabase GetDatabase(string name) {
            if (name == DatabaseName)
                return database;
            return extensionDbs[name];
        }

        internal Dictionary<string, EntityDatabase> GetDatabases() {
            var result = new Dictionary<string, EntityDatabase> (extensionDbs.Count + 1) {
                { database.name, database }
            };
            foreach (var extensionDB in extensionDbs) {
                result.Add(extensionDB.Key, extensionDB.Value);
            }
            return result;
        }
        #endregion
    }
}
