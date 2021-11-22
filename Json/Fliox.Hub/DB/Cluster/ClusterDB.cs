// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema;
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
        private  readonly   DatabaseSchema      databaseSchema; // not really required as db is readonly - but enables exposing schema

        public   override   string              ToString() => name;

        public const string Name = "cluster";
        
        public ClusterDB (FlioxHub hub, string name = null, DbOpt opt = null)
            : base (new ClusterHandler(hub), opt)
        {
            this.hub        = hub  ?? throw new ArgumentNullException(nameof(hub));
            this.name       = name ?? Name;
            typeSchema      = new NativeTypeSchema(typeof(ClusterStore));
            databaseSchema  = new DatabaseSchema(typeSchema);
            stateDB         = new MemoryDatabase();
            stateDB.Schema  = databaseSchema;
            clusterHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override void Dispose() {
            base.Dispose();
            databaseSchema.Dispose();
            typeSchema.Dispose();
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        public override async Task ExecuteSyncPrepare(SyncRequest syncRequest, MessageContext messageContext) {
            var pool = messageContext.pool;
            using (var pooled  = pool.Type(() => new ClusterStore(clusterHub)).Get()) {
                var cluster = pooled.instance;
                var tasks = syncRequest.tasks;
                cluster.UpdateCatalogs  (hub, tasks);
                
                await cluster.SyncTasks().ConfigureAwait(false);
            }
        }
        
        public override DatabaseInfo GetDatabaseInfo() {
            return stateDB.GetDatabaseInfo();
        }
        
        internal static bool FindTask(string container, List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                if (task is ReadEntities read && read.container == container)
                    return true;
                if (task is QueryEntities query && query.container == container)
                    return true;
            }
            return false;
        }
    }
    
    public partial class ClusterStore
    {
        internal void UpdateCatalogs(FlioxHub hub, List<SyncRequestTask> tasks) {
            var databases = hub.GetDatabases();
            foreach (var pair in databases) {
                var database        = pair.Value;
                var databaseName    = pair.Key;
                var databaseInfo    = database.GetDatabaseInfo();
                if (ClusterDB.FindTask(nameof(catalogs), tasks)) {
                    var catalog = new Catalog {
                        id              = databaseName,
                        databaseType    = databaseInfo.databaseType,
                        containers      = databaseInfo.containers,
                    };
                    catalogs.Upsert(catalog);
                }
                if (ClusterDB.FindTask(nameof(schemas), tasks)) {
                    var schema = CreateCatalogSchema(databaseInfo, databaseName);
                    if (schema != null)
                        schemas.Upsert(schema);
                }
            }
        }
        
        internal static CatalogSchema CreateCatalogSchema (DatabaseInfo databaseInfo, string databaseName) {
            if (databaseInfo.schema == null)
                return null;
            var typeSchema  = databaseInfo.schema.typeSchema;
            var schemaType  = typeSchema.RootType;
            var entityTypes = typeSchema.GetEntityTypes();
            var generator   = new Generator(typeSchema, ".json", null, entityTypes);
            JsonSchemaGenerator.Generate(generator);
            var jsonSchemas = new Dictionary<string, JsonValue>();
            foreach (var pair in generator.files) {
                jsonSchemas.Add(pair.Key,new JsonValue(pair.Value));
            }
            var schema = new CatalogSchema {
                id          = databaseName,
                schemaName  = schemaType.Name,
                schemaPath  = schemaType.Path + generator.fileExt,
                jsonSchemas = jsonSchemas
            };
            return schema;
        }
    }
}
