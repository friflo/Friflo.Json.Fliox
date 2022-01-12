// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Host.Stats;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="FlioxHub"/> instance is the single entry point used to handle <b>all</b> requests send by a client -
    /// e.g. a <see cref="Client.FlioxClient"/>. When instantiating a <see cref="FlioxHub"/> a default
    /// <see cref="EntityDatabase"/> is assigned to the instance and all operations / tasks requested by a client
    /// are applied to this <see cref="database"/>.
    /// <br/>
    /// A <see cref="FlioxHub"/> instance handle client requests by its <see cref="ExecuteSync"/> method.
    /// A request is represented by a <see cref="SyncRequest"/> and its <see cref="SyncRequest.tasks"/> are executed
    /// on the given <see cref="SyncRequest.database"/>.
    /// If database == null the default <see cref="database"/> of <see cref="FlioxHub"/> is used.
    /// <br/>
    /// The <see cref="SyncRequest.tasks"/> contains all database operations like create, read, upsert, delete
    /// and all messages / commands send by a client. 
    /// The <see cref="FlioxHub"/> execute these tasks by the <see cref="EntityDatabase.handler"/> of the
    /// specified <see cref="database"/>.
    /// <br/>
    /// Instances of <see cref="FlioxHub"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Client.FlioxClient"/> operating on the same <see cref="FlioxHub"/> instance.
    /// To maintain thread safety <see cref="FlioxHub"/> implementations must not have any mutable state.
    /// <br/>
    /// The <see cref="FlioxHub"/> can be configured to support.
    /// <list type="bullet">
    ///   <item>
    ///     A Pub-Sub implementation to send events of database changes or messages to clients by assigning an
    ///     <see cref="Event.EventBroker"/> to <see cref="EventBroker"/>.
    ///     Note: A client need to subscribe events in order to receive them. 
    ///   </item>
    ///   <item>
    ///     User authentication and task authorization. Each task is authorized individually. To enable this assign 
    ///     a <see cref="DB.UserAuth.UserAuthenticator"/> to <see cref="Authenticator"/>.
    ///   </item>
    ///   <item>
    ///     Monitoring of database access (requests) by adding a <see cref="MonitorDB"/> with
    ///     <see cref="AddExtensionDB"/>.
    ///   </item>
    /// </list>
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FlioxHub : IDisposable
    {
        public   readonly   EntityDatabase      database;      
        /// <summary>
        /// An optional <see cref="Event.EventBroker"/> used to enable Pub-Sub. If enabled the database send
        /// events to a client for database changes and messages the client has subscribed.
        /// In case of remote database connections WebSockets are used to send Pub-Sub events to clients.   
        /// </summary>
        public              EventBroker         EventBroker     { get; set; }
        
        /// <summary>
        /// An <see cref="Auth.Authenticator"/> performs authentication and authorization for all
        /// <see cref="SyncRequest.tasks"/> in a <see cref="SyncRequest"/> sent by a client.
        /// All successful authorized <see cref="SyncRequest.tasks"/> are executed by the <see cref="EntityDatabase.handler"/>.
        /// </summary>
        public              Authenticator       Authenticator   { get => authenticator; set => authenticator = NotNull(value, nameof(Authenticator)); }
        
        /// <summary>
        /// <see cref="ClientController"/> is used to create / add unique client ids to enable sending events to
        /// specific user clients.
        /// It also enables monitoring execution statistics of <see cref="FlioxHub.ExecuteSync"/> 
        /// </summary>
        public              ClientController    ClientController{ get => clientController; set => clientController = NotNull(value, nameof(ClientController)); }
        
        private  static     T NotNull<T> (T value, string name) where T : class => value ?? throw new NullReferenceException(name);

        /// <summary>
        /// A host name that is assigned to a default database.
        /// Its only purpose is to use it as id in <see cref="DB.Monitor.HostInfo.id"/>.
        /// </summary>
        /// 
        public   readonly   string              hostName;
        public              string              description;
        public              string              website;
        
        public   readonly   string              Version = "0.0.1";
        public   readonly   SharedEnv           sharedEnv;
        
        internal readonly   HostStats           hostStats = new HostStats();
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private             Authenticator       authenticator       = new AuthenticateNone(new AuthorizeAllow());
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private             ClientController    clientController    = new IncrementClientController();

        
        /// <summary>
        /// Construct a <see cref="FlioxHub"/> with the given default database.
        /// database can be null in case of using a <see cref="FlioxHub"/> instance without a default database. 
        /// </summary>
        public FlioxHub (EntityDatabase database, SharedEnv env = null, string hostName = null) {
            sharedEnv       = env  ?? SharedEnv.Default;
            this.database   = database;
            this.hostName   = hostName ?? "host";
        }
        
        public FlioxHub SetInfo(string description, string website = null) {
            this.description    = description;
            this.website        = website;
            return this;
        }        
       
        public   virtual    void    AddEventTarget      (in JsonKey clientId, IEventTarget eventTarget) {}
        public   virtual    void    RemoveEventTarget   (in JsonKey clientId) {}

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
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.Hub.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </summary>
        public virtual async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            messageContext.hub          = this;
            var dbName = syncRequest.database;
            if (dbName == EntityDatabase.MainDB)
                dbName = null;
            messageContext.DatabaseName = dbName;
            if (messageContext.authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            messageContext.clientId = syncRequest.clientId;
            
            await authenticator.Authenticate(syncRequest, messageContext).ConfigureAwait(false);
            messageContext.clientIdValidation = authenticator.ValidateClientId(clientController, messageContext);
            
            var requestTasks = syncRequest.tasks;
            if (requestTasks == null) {
                return new ExecuteSyncResult ("missing field: tasks (array)", ErrorResponseType.BadRequest);
            }
            EntityDatabase db = database;
            if (dbName != null) {
                if (!extensionDbs.TryGetValue(dbName, out db))
                    return new ExecuteSyncResult($"database not found: '{syncRequest.database}'", ErrorResponseType.BadRequest);
                await db.ExecuteSyncPrepare(syncRequest, messageContext).ConfigureAwait(false);
            }
            var tasks       = new List<SyncTaskResult>(requestTasks.Count);
            var resultMap   = new Dictionary<string, ContainerEntities>();
            var response    = new SyncResponse { tasks = tasks, resultMap = resultMap, database = dbName };
            var taskHandler = db.handler;
            
            for (int index = 0; index < requestTasks.Count; index++) {
                var task = requestTasks[index];
                if (task == null) {
                    tasks.Add(SyncRequestTask.InvalidTask($"element must not be null. tasks[{index}]"));
                    continue;
                }
                task.index = index;
                try {
                    var result = await taskHandler.ExecuteTask(task, db, response, messageContext).ConfigureAwait(false);
                    tasks.Add(result);
                } catch (Exception e) {
                    tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                }
            }
            hostStats.Update(syncRequest);
            UpdateRequestStats(dbName, syncRequest, messageContext);

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
            return new ExecuteSyncResult(response);
        }
        
        private static TaskErrorResult TaskExceptionError (Exception e) {
            var exceptionName   = e.GetType().Name;
            var msg             = $"{exceptionName}: {e.Message}";
            var stack           = e.StackTrace;
#if UNITY_5_3_OR_NEWER
            if (stack != null) {
                // Unity add StackTrace sections starting with:
                // --- End of stack trace from previous location where exception was thrown ---
                // Remove these sections as they bloat the stacktrace assuming the relevant part of the stacktrace
                // is at the beginning.
                var endOfStackTraceFromPreviousLocation = stack.IndexOf("\n--- End of stack", StringComparison.Ordinal);
                if (endOfStackTraceFromPreviousLocation != -1) {
                    stack = stack.Substring(0, endOfStackTraceFromPreviousLocation);
                }
            }
#endif
            return new TaskErrorResult (TaskErrorResultType.UnhandledException,msg, stack);
        }

        private void UpdateRequestStats(string database, SyncRequest syncRequest, MessageContext messageContext) {
            if (database == null) database = EntityDatabase.MainDB;
            var user = messageContext.User;
            RequestCount.UpdateCounts(user.requestCounts, database, syncRequest);
            ref var clientId = ref messageContext.clientId;
            if (clientId.IsNull())
                return;
            if (clientController.clients.TryGetValue(clientId, out UserClient client)) {
                RequestCount.UpdateCounts(client.requestCounts, database, syncRequest);
            }
        }

        // --- extension databases
        private readonly   Dictionary<string, EntityDatabase> extensionDbs = new Dictionary<string, EntityDatabase>();
        
        public void AddExtensionDB(string name, EntityDatabase extensionDB) {
            extensionDbs.Add(name, extensionDB);
        }
        
        public bool TryGetDatabase(string name, out EntityDatabase value) {
            if (name == null || name == EntityDatabase.MainDB) {
                value = database;
                return true;
            }
            return extensionDbs.TryGetValue(name, out value);
        }
        
        public EntityDatabase GetDatabase(string name) {
            if (name == null)
                return database;
            return extensionDbs[name];
        }

        internal Dictionary<string, EntityDatabase> GetDatabases() {
            var result = new Dictionary<string, EntityDatabase> (extensionDbs.Count + 1);
            if (database != null) {
                result.Add(EntityDatabase.MainDB, database);
            }
            foreach (var extensionDB in extensionDbs) {
                result.Add(extensionDB.Key, extensionDB.Value);
            }
            return result;
        }

        public virtual void Dispose() { }  // todo - remove
    }
    
    // --------------------------------- ExecuteSyncResult ---------------------------------
    public readonly struct ExecuteSyncResult {
        public   readonly   SyncResponse    success;
        public   readonly   ErrorResponse   error;

        public ExecuteSyncResult (SyncResponse successResponse) {
            success = successResponse;
            error   = null;
        }
        
        public ExecuteSyncResult (string errorMessage, ErrorResponseType errorType) {
            success = null;
            error   = new ErrorResponse { message = errorMessage, type = errorType };
        }
        
        public  ProtocolResponse Result { get {
            if (success != null)
                return success;
            return error;
        } }

        public override string ToString() => success != null ? success.ToString() : error.ToString();
    }
}
