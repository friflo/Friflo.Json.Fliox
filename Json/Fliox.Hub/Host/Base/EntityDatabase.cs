// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
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
        public              DatabaseSchema      Schema          { get; init; }
        
        public  virtual     Task<SyncConnection> GetConnectionAsync()  => throw new NotImplementedException();
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
        protected EntityDatabase(string dbName, DatabaseService service){
            containers      = new ConcurrentDictionary<ShortString, EntityContainer>(ShortString.Equality);
            name            = dbName ?? throw new ArgumentNullException(nameof(dbName));
            nameShort       = new ShortString(dbName);
            this.service    = service ?? new DatabaseService();
        }
        
        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        public EntityDatabase AddCommands(ICommands commands) {
            if (!service.AddAttributedHandlers(commands, out var error)) {
                throw new InvalidOperationException(error);
            }
            return this;
        }
        #endregion
        
    #region - general public methods
        /// <summary>
        /// return true to execute given <paramref name="task"/> synchronous. <br/>
        /// return false to execute the <paramref name="task"/> asynchronous
        /// </summary>
        public   virtual    bool                IsSyncTask(SyncRequestTask task) => false;
    
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
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
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
        #endregion

    #region - seed database
        /// <summary>Seed the database with content of the given <paramref name="src"/> database</summary>
        /// <remarks>
        /// If given database has no schema the key name of all entities in all containers need to be "id"
        /// </remarks>
        public async Task SeedDatabase(EntityDatabase src, int? maxCount = null) {
            if (Schema == null) throw new InvalidOperationException("SeedDatabase requires a Schema");
            var sharedEnv       = new SharedEnv();
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            var syncContext     = new SyncContext(sharedEnv, null, memoryBuffer);
            try {
                syncContext.database = this;
                var entityTypes     = Schema.typeSchema.GetEntityTypes();
                foreach (var pair in entityTypes) {
                    var container   = pair.Key;
                    string keyName  = pair.Value.KeyField.name;
                    await SeedContainer(src, container, keyName, maxCount, syncContext).ConfigureAwait(false);
                }
            }
            finally {
                syncContext.CloseConnection();
            }
        }
        
        private async Task SeedContainer(EntityDatabase src, string container, string keyName, int? maxCount, SyncContext syncContext)
        {
            var containerName   = new ShortString(container);
            var srcContainer    = src.GetOrCreateContainer(containerName);
            var dstContainer    = GetOrCreateContainer(containerName);
            var filterContext   = new OperationContext();
            filterContext.Init(Operation.FilterTrue, out _);
            var query           = new QueryEntities {
                container       = containerName,
                filterContext   = filterContext,
                keyName         = keyName,
                maxCount        = maxCount,
            };
            while (true) {
                var queryResult = await srcContainer.QueryEntitiesAsync(query, syncContext).ConfigureAwait(false);

                var entities    = new List<JsonEntity>(queryResult.entities.Length);
                foreach (var entity in queryResult.entities) {
                    entities.Add(new JsonEntity(entity.Json));
                }
                if (!KeyValueUtils.GetKeysFromEntities (keyName, entities, syncContext.sharedEnv, out string error)) {
                    throw new InvalidOperationException($"seeding data error. container: {container}, error: {error}");
                }
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
                syncContext.CloseConnection();
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
