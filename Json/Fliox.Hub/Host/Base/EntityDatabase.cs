// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public delegate string CustomContainerName(string name);
    
    /// <summary>Specify the column schema of an SQL table</summary>
    public enum TableType
    {
        /// <summary>store a document in a single JSON column</summary>
        JsonColumn  = 0,
        /// <summary>store each member of a document in a separate typed column</summary>
        Relational  = 1,
    }
    
    public sealed class PrepareDatabaseException : Exception {
        public PrepareDatabaseException(string message) : base (message) { } 
    }
    
    /// <summary>
    /// A set of options used in <see cref="EntityDatabase.SetupDatabaseAsync"/><br/>
    /// </summary>
    [Flags]
    public enum Setup
    {
        /// <summary>Create a database if not exists</summary>
        CreateDatabase      = 1,
        /// <summary>Create all tables missing in the database</summary>
        CreateTables        = 2,
        /// <summary>Create missing virtual - computed - columns for a database with <see cref="TableType.JsonColumn"/></summary>
        AddVirtualColumns   = 3,
        /// <summary>Create missing columns for a database with <see cref="TableType.JsonColumn"/></summary>
        AddColumns          = 4,
        //
        Default = CreateDatabase | CreateTables | AddVirtualColumns | AddColumns
    }
    
    /// <summary>
    /// <see cref="EntityDatabase"/> is the abstraction for specific database adapter / implementation e.g. a
    /// <see cref="MemoryDatabase"/> or <see cref="FileDatabase"/>.
    /// </summary>
    /// <remarks>
    /// An <see cref="EntityDatabase"/> contains multiple <see cref="EntityContainer"/>'s each representing
    /// a table / collection of a database. Each container is intended to store the records / entities of a specific type.
    /// E.g. one container for storing JSON objects representing 'articles' another one for storing 'orders'.
    /// <br/>
    /// Optionally a <see cref="DatabaseSchema"/> can be assigned to the database via the property <see cref="Schema"/>.
    /// This enables Type / schema validation of JSON entities written (create, update and patch) to its containers.
    /// <br/>
    /// Instances of <see cref="EntityDatabase"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Client.FlioxClient"/> operating on the same <see cref="EntityDatabase"/> instance
    /// - used by a <see cref="FlioxHub"/>.
    /// To maintain thread safety <see cref="EntityDatabase"/> implementations must not have any mutable state.
    /// </remarks>
    public abstract class EntityDatabase : IDisposable
    {
    #region - members
        /// <summary>database name</summary>
        public   readonly   string              name;                   // not null
        /// <summary>database name encoded as type <see cref="ShortString"/></summary>
        [DebuggerBrowsable(Never)]
        public   readonly   ShortString         nameShort;              // not null
        public   override   string              ToString()  => name;    // not null
        
        /// <summary> map of of containers identified by their container name </summary>
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<ShortString, EntityContainer>  containers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EntityContainer>                        Containers => containers.Values;
        
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; }
        
        /// <summary>A mapping function used to assign a custom container name.</summary>
        /// <remarks>
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="EntityContainer"/> it enables uniform mapping across different
        /// <see cref="EntityContainer"/> implementations.
        /// </remarks>
        [DebuggerBrowsable(Never)]
        public          CustomContainerName CustomContainerName { get; init; } = DefaultCustomContainerName;

        private static readonly CustomContainerName DefaultCustomContainerName = name => name;
        
        /// <summary>
        /// The <see cref="service"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// An <see cref="EntityDatabase"/> implementation can assign as custom handler by its constructor
        /// </summary>
        internal readonly   DatabaseService     service;    // never null
        /// <summary>name of the storage type. E.g. <c>in-memory, file-system, remote, Cosmos, ...</c></summary>
        public   abstract   string              StorageType  { get; }
        
        #endregion
        
    #region - initialize
        /// <summary>
        /// constructor parameters are mandatory to force implementations having them in their constructors also or
        /// pass null by implementations.
        /// </summary>
        protected EntityDatabase(string dbName, DatabaseSchema schema, DatabaseService service){
            containers      = new ConcurrentDictionary<ShortString, EntityContainer>(ShortString.Equality);
            name            = dbName ?? throw new ArgumentNullException(nameof(dbName));
            nameShort       = new ShortString(dbName);
            this.Schema     = schema;
            this.service    = service ?? new DatabaseService();
        }
        
        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        protected void ClearContainers() {
            containers.Clear();
        }
        
        /// <summary>
        /// Create a ready-to-use database or modify the schema of an existing database.<br/>
        /// <br/>
        /// <b>Note:</b> This method is intended for development to create a database and update its tables based on its <see cref="Schema"/>.<br/>
        /// In a production environment this method should be called from a migration script - not from a service.
        /// </summary>
        /// <remarks>
        /// <i>Note</i>: <see cref="SetupDatabaseAsync"/> support only additive database operations to prevent data loss.<br/>
        /// Dropping tables or columns need to be done with appropriate database tools.<br/>
        /// <br/> 
        /// The parameter <paramref name="options"/> define which database operations are executed.
        /// <list type="bullet">
        ///     <item>Create a database if not exist</item>
        ///     <item>Create tables which not exist</item>
        ///     <item>Add missing virtual columns if using a database with <see cref="TableType.JsonColumn"/> tables</item>
        ///     <item>Add missing columns if using a database with <see cref="TableType.Relational"/> tables</item>
        /// </list>
        /// </remarks>
        public async Task<EntityDatabase> SetupDatabaseAsync(Setup options = Setup.Default) {
            ISyncConnection connection = null;
            try {
                connection = await GetConnectionAsync().ConfigureAwait(false);
                if (!connection.IsOpen) {
                    try {
                        if ((options & Setup.CreateDatabase) == 0) {
                            throw new PrepareDatabaseException(connection.Error.message);
                        }
                        await CreateDatabaseAsync().ConfigureAwait(false);
                        connection = await GetConnectionAsync().ConfigureAwait(false);
                    } catch (Exception e) {
                        throw new PrepareDatabaseException(e.Message);
                    }
                }
                await SetupInternal(options, connection).ConfigureAwait(false);
            }
            finally {
                connection?.Dispose();
                connection?.ClearPool();
            }
            return this;
        }
        
        public async Task DropContainerAsync(string name) {
            using var connection = await GetConnectionScopeAsync().ConfigureAwait(false);
            await DropContainerAsync(connection.instance, name).ConfigureAwait(false);
        }
        
        public async Task DropAllContainersAsync() {
            var schema              = Schema;
            var containerNames      = schema != null ? schema.GetContainers() : await GetContainers().ConfigureAwait(false);
            using var connection    = await GetConnectionScopeAsync().ConfigureAwait(false);
            foreach (var containerName in containerNames) {
                await DropContainerAsync(connection.instance, containerName).ConfigureAwait(false);
            }
        }
        
        private async Task SetupInternal(Setup options, ISyncConnection connection)
        {
            if (this is ISQLDatabase sqlDatabase) {
                await sqlDatabase.CreateFunctions(connection).ConfigureAwait(false);
            }
            var containerNames = Schema.GetContainers();
            var tables = new List<ISQLTable>();
            foreach (var containerName in containerNames) {
                var container = CreateContainer(new ShortString(containerName), this);
                if (container is not ISQLTable table) {
                    continue;
                }
                tables.Add(table);
            }
            if ((options & Setup.CreateTables) != 0) {
                foreach (var table in tables) {
                    var result = await table.CreateTable(connection).ConfigureAwait(false);
                    if (result.Failed) {
                        throw new PrepareDatabaseException(result.error);
                    }      
                }
            }
            if ((options & Setup.AddVirtualColumns) != 0) {
                foreach (var table in tables) {
                    var result = await table.AddVirtualColumns(connection).ConfigureAwait(false);
                    if (result.Failed) {
                        throw new PrepareDatabaseException(result.error);
                    }      
                }
            }
            if ((options & Setup.AddColumns) != 0) {
                foreach (var table in tables) {
                    var result = await table.AddColumns(connection).ConfigureAwait(false);
                    if (result.Failed) {
                        throw new PrepareDatabaseException(result.error);
                    }      
                }
            }
        }
        
        protected   virtual Task    CreateDatabaseAsync()           => Task.CompletedTask;
        public      virtual Task    DropDatabaseAsync()             => throw new NotSupportedException($"DropDatabaseAsync() not supported");
        protected   virtual Task    DropContainerAsync(ISyncConnection connection, string name) => throw new NotSupportedException($"DropContainerAsync() not supported");
        
        public EntityDatabase AddCommands(IServiceCommands commands) {
            if (!service.AddAttributedHandlers(commands, out var error)) {
                throw new InvalidOperationException(error);
            }
            return this;
        }
        
        protected static DatabaseSchema AssertSchema<TDatabase>(DatabaseSchema schema) {
            return schema ?? throw new ArgumentNullException(nameof(schema), $"{typeof(TDatabase).Name} requires a {nameof(DatabaseSchema)}");
        }
        
        #endregion
        
    #region - general public methods
        /// <summary>
        /// return true to execute given <paramref name="task"/> synchronous. <br/>
        /// return false to execute the <paramref name="task"/> asynchronous
        /// </summary>
        public   virtual    bool  IsSyncTask(SyncRequestTask task, in PreExecute execute) => false;
    
        /// <summary>Create a container with the given <paramref name="name"/> in the database</summary>
        public abstract EntityContainer CreateContainer     (in ShortString name, EntityDatabase database);
        
        internal void AddContainer(EntityContainer container) {
            containers.TryAdd(container.nameShort, container);
        }
        
        protected bool TryGetContainer(in ShortString name, out EntityContainer container) {
            return containers.TryGetValue(name, out container);
        }

        /// <summary>
        /// return the <see cref="EntityContainer"/> with the given <paramref name="name"/>.
        /// Create a new <see cref="EntityContainer"/> if not already done.
        /// </summary>
        public EntityContainer GetOrCreateContainer(in ShortString name)
        {
            if (containers.TryGetValue(name, out EntityContainer container)) {
                return container;
            }
            var schema = Schema;
            if (schema != null && !schema.HasContainer(name)) {
                return null;
            }
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        protected EntityContainer[] GetContainersSync() {
            var containerList = new EntityContainer[containers.Count];
            int n = 0;
            foreach (var pair in containers) {
                containerList[n++] = pair.Value;
            }
            return containerList;
        }
        
        protected virtual Task<string[]> GetContainers() {
            var containerList = new string[containers.Count];
            int n = 0;
            foreach (var pair in containers) {
                containerList[n++] = pair.Value.name;
            }
            return Task.FromResult(containerList);
        }
        
        /// <summary>return all database containers</summary>
        public async Task<DbContainers> GetDbContainers(string database, FlioxHub hub) {
            string[] containerList;
            var schema = Schema;
            if (schema != null) {
                containerList = schema.GetContainers();
            } else {
                containerList = await GetContainers().ConfigureAwait(false);
            }
            bool? isDefaultDB = null;
            if (database == hub.database.name) {
                isDefaultDB = true;
            }
            return new DbContainers {
                containers  = containerList,
                storage     = StorageType,
                id          = database,
                defaultDB   = isDefaultDB
            };
        }

        private static class Static {
            internal const bool ExposeSchemaCommands = true; // false for debugging
        }

        /// <summary>return all database messages and commands</summary>
        public DbMessages GetDbMessages() {
            string[] commands;
            string[] messages;
            var schema = Schema;
            if (Static.ExposeSchemaCommands && schema != null) {
                commands = schema.GetCommands();
                messages = schema.GetMessages();
            } else {
                commands = service.GetCommands();
                messages = service.GetMessages();
            }
            return new DbMessages { commands = commands, messages = messages };
        }
        
        public virtual Result<RawSqlResult> ExecuteRawSQL(RawSql sql, SyncContext syncContext) {
            return Result.Error($"Not supported for database provider: {StorageType}");
        }
        
        public virtual Task<Result<RawSqlResult>> ExecuteRawSQLAsync(RawSql sql, SyncContext syncContext) {
            var result = ExecuteRawSQL(sql, syncContext);
            return Task.FromResult(result);
        }

        #endregion
        
    #region - sync connection
        protected internal virtual  Task<ISyncConnection>   GetConnectionAsync()                            => Task.FromResult<ISyncConnection>(new DefaultSyncConnection());
        protected internal virtual  ISyncConnection         GetConnectionSync()                             => throw new NotImplementedException();
        protected internal virtual  void                    ReturnConnection(ISyncConnection connection)    => connection.Dispose();
        protected internal virtual  TransResult             Transaction     (SyncContext syncContext, TransCommand command) => new TransResult(command);
        protected internal virtual  Task<TransResult>       TransactionAsync(SyncContext syncContext, TransCommand command) => Task.FromResult(new TransResult(command));

        private async Task<ConnectionScope> GetConnectionScopeAsync() {
            var connection = await GetConnectionAsync().ConfigureAwait(false);
            if (connection.IsOpen) {
                return new ConnectionScope(connection, this);
            }
            throw new InvalidOperationException($"connect failed: {connection.Error.message}");
        }
        #endregion
        
    #region - seed database
        /// <summary>Seed the database with content of the given <paramref name="src"/> database</summary>
        /// <remarks>
        /// If given database has no schema the key name of all entities in all containers need to be "id"
        /// </remarks>
        public async Task<EntityDatabase> SeedDatabase(EntityDatabase src, int? maxCount = null) {
            if (Schema == null) throw new InvalidOperationException("SeedDatabase requires a Schema");
            var sharedEnv       = new SharedEnv();
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            var syncContext     = new SyncContext(sharedEnv, null, memoryBuffer);
            try {
                syncContext.database = this;
                var entityTypes     = Schema.typeSchema.GetEntityTypes();
                foreach (var pair in entityTypes) {
                    var container   = pair.Key;
                    await SeedContainer(src, container, maxCount, syncContext).ConfigureAwait(false);
                }
            }
            finally {
                syncContext.ReturnConnection();
            }
            return this;
        }
        
        private async Task SeedContainer(EntityDatabase src, string container, int? maxCount, SyncContext syncContext)
        {
            var containerName   = new ShortString(container);
            var srcContainer    = src.GetOrCreateContainer(containerName);
            var dstContainer    = GetOrCreateContainer(containerName);
            var filterContext   = new OperationContext();
            filterContext.Init(Operation.FilterTrue, out _);
            var query           = new QueryEntities {
                container       = containerName,
                filterContext   = filterContext,
                maxCount        = maxCount,
            };
            while (true) {
                var queryResult = await srcContainer.QueryEntitiesAsync(query, syncContext).ConfigureAwait(false);

                var values      = queryResult.entities.Values;
                var entities    = new List<JsonEntity>(values.Length);
                foreach (var entity in values) {
                    entities.Add(new JsonEntity(entity.key, entity.Json));
                }
                /* if (!KeyValueUtils.GetKeysFromEntities (keyName, entities, syncContext.sharedEnv, out string error)) {
                    throw new InvalidOperationException($"seeding data error. container: {container}, error: {error}");
                } */
                var upsert = new UpsertEntities { container = containerName, entities = entities };
                var result = await dstContainer.UpsertEntitiesAsync(upsert, syncContext).ConfigureAwait(false);
                var upsertError = result.Error; 
                if (upsertError != null) {
                    throw new InvalidOperationException($"seeding upsert error. container: {container}, error: {upsertError.message}");
                }
                if (queryResult.cursor == null) {
                    return;
                }
                query.cursor = queryResult.cursor;
            }
        }
        
        public async Task ClearDatabase() {
            if (Schema == null) throw new InvalidOperationException("ClearDatabase requires a Schema");
            var sharedEnv       = new SharedEnv();
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            var syncContext     = new SyncContext(sharedEnv, null, memoryBuffer);
            try {
                syncContext.database = this;
                var entityTypes     = Schema.typeSchema.GetEntityTypes();
                foreach (var pair in entityTypes) {
                    var container = pair.Key;
                    await ClearContainer(container, syncContext).ConfigureAwait(false);
                }
            }
            finally {
                syncContext.ReturnConnection();
            }
        }
        
        private async Task ClearContainer(string container, SyncContext syncContext)
        {
            var containerName   = new ShortString(container);
            var dstContainer    = GetOrCreateContainer(containerName);
            
            var delete          = new DeleteEntities { container = containerName, all = true };
            await dstContainer.DeleteEntitiesAsync(delete, syncContext).ConfigureAwait(false);
        }
        #endregion
    }
}
