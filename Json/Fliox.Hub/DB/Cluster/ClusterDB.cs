// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class ClusterDB : EntityDatabase
    {
        // --- private / internal
        internal readonly   EntityDatabase      stateDB;
        private  readonly   FlioxHub            clusterHub;
        private  readonly   FlioxHub            hub;
        private  readonly   string              name;
        private  readonly   NativeTypeSchema    typeSchema;     // not really required as db is readonly - but enables exposing schema

        public   override   string              ToString()  => name;
        public   override   string              StorageName => stateDB.StorageName;

        public const string Name = "cluster";
        
        public ClusterDB (FlioxHub hub, string name = null, DbOpt opt = null)
            : base (new ClusterHandler(), opt)
        {
            this.hub        = hub  ?? throw new ArgumentNullException(nameof(hub));
            this.name       = name ?? Name;
            typeSchema      = new NativeTypeSchema(typeof(ClusterStore));
            Schema          = new DatabaseSchema(typeSchema);
            stateDB         = new MemoryDatabase(null, MemoryContainerType.NonConcurrent);
            clusterHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override void Dispose() {
            base.Dispose();
            typeSchema.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        public override async Task ExecuteSyncPrepare(SyncRequest syncRequest, ExecuteContext executeContext) {
            var pool = executeContext.pool;
            using (var pooled  = pool.Type(() => new ClusterStore(clusterHub)).Get()) {
                var cluster = pooled.instance;
                var tasks = syncRequest.tasks;
                await cluster.UpdateCatalogs  (hub, tasks).ConfigureAwait(false);
                
                await cluster.SyncTasks().ConfigureAwait(false);
            }
        }
        
        internal static bool FindTask(string container, JsonKey dbKey, List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                if (task is ReadEntities read && read.container == container) {
                    return read.sets.Any(set => set.ids.Contains(dbKey));
                }
                if (task is QueryEntities query && query.container == container)
                    return true;
            }
            return false;
        }
    }
    
    public partial class ClusterStore
    {
        internal async Task UpdateCatalogs(FlioxHub hub, List<SyncRequestTask> tasks) {
            var hubDbs = hub.GetDatabases();
            foreach (var pair in hubDbs) {
                var database        = pair.Value;
                var databaseName    = pair.Key;
                var dbKey           = new JsonKey(databaseName);
                if (ClusterDB.FindTask(nameof(containers), dbKey, tasks)) {
                    var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
                    dbContainers.id     = databaseName;
                    containers.Upsert(dbContainers);
                }
                if (ClusterDB.FindTask(nameof(messages), dbKey, tasks)) {
                    var dbMessages  = database.GetDbMessages();
                    dbMessages.id   = databaseName;
                    messages.Upsert(dbMessages);
                }
                if (ClusterDB.FindTask(nameof(schemas),dbKey, tasks)) {
                    var schema = CreateCatalogSchema(database, databaseName);
                    if (schema != null)
                        schemas.Upsert(schema);
                }
            }
        }
        
        internal static DbSchema CreateCatalogSchema (EntityDatabase database, string databaseName) {
            var databaseSchema = database.Schema;
            if (databaseSchema == null)
                return null;
            var jsonSchemas = databaseSchema.GetJsonSchemas();
            var schema = new DbSchema {
                id          = databaseName,
                schemaName  = databaseSchema.Name,
                schemaPath  = databaseSchema.Path,
                jsonSchemas = jsonSchemas
            };
            return schema;
        }
        
        internal static async Task<HostCluster> GetDbList (FlioxHub hub) {
            var databases = hub.GetDatabases();
            var catalogs = new List<DbContainers>(databases.Count);
            foreach (var pair in databases) {
                var database        = pair.Value;
                var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
                dbContainers.id     = pair.Key;
                catalogs.Add(dbContainers);
            }
            return new HostCluster{ databases = catalogs };
        }
    }
}
