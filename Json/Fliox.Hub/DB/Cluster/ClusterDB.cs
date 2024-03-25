// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /// <summary>
    /// <see cref="ClusterDB"/> store information about databases hosted by the Hub: <br/>
    /// - available containers aka tables per database <br/>
    /// - available commands per database <br/>
    /// - the schema assigned to each database
    /// </summary>
    public sealed class ClusterDB : EntityDatabase
    {
        // --- private / internal
        internal readonly   EntityDatabase      stateDB;
        internal readonly   FlioxHub            clusterHub;
        internal readonly   FlioxHub            hub;

        public   override   string              StorageType => stateDB.StorageType;

        private static readonly DatabaseSchema ClusterSchema = DatabaseSchema.CreateFromType(typeof(ClusterStore));

        public ClusterDB (string dbName, FlioxHub hub)
            : base (dbName, ClusterSchema, new ClusterService())
        {
            ((ClusterService)service).clusterDB = this;
            this.hub        = hub  ?? throw new ArgumentNullException(nameof(hub));
            stateDB         = new MemoryDatabase(dbName) { ContainerType = MemoryType.NonConcurrent };
            clusterHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        internal static bool FindTask(string container, in JsonKey dbKey, ListOne<SyncRequestTask> tasks) {
            var containerName = new ShortString(container);
            foreach (var task in tasks.GetReadOnlySpan()) {
                if (task is ReadEntities read && read.container.IsEqual(containerName)) {
                    return read.ids.Contains(dbKey, JsonKey.Equality);
                }
                if (task is QueryEntities query && query.container.IsEqual(containerName))
                    return true;
            }
            return false;
        }
    }
    
    public partial class ClusterStore
    {
        internal void UpdateClusterDB(FlioxHub hub, ListOne<SyncRequestTask> tasks) {
            var hubDbs = hub.GetDatabases();
            foreach (var pair in hubDbs) {
                var database        = pair.Value;
                var databaseName    = pair.Key;
                var dbKey           = new JsonKey(databaseName);
                if (ClusterDB.FindTask(nameof(containers), dbKey, tasks)) {
                    // synchronous call OK => ClusterDB is a memory database - it tasks are executed synchronous
                    var dbContainers    = database.GetDbContainers(databaseName, hub).Result;
                    containers.Upsert(dbContainers);
                }
                if (ClusterDB.FindTask(nameof(messages), dbKey, tasks)) {
                    var dbMessages  = database.GetDbMessages();
                    dbMessages.id   = databaseName;
                    messages.Upsert(dbMessages);
                }
                if (ClusterDB.FindTask(nameof(schemas),dbKey, tasks)) {
                    var schema = CreateDbSchema(database);
                    if (schema != null)
                        schemas.Upsert(schema);
                }
            }
        }
        
        internal static DbSchema CreateDbSchema (EntityDatabase database) {
            var databaseSchema = database.Schema;
            if (databaseSchema == null)
                return null;
            var jsonSchemas = databaseSchema.GetJsonSchemas();
            jsonSchemas.Remove("openapi.json");
            var schema = new DbSchema {
                id          = database.name,
                schemaName  = databaseSchema.Name,
                schemaPath  = databaseSchema.Path,
                jsonSchemas = jsonSchemas
            };
            return schema;
        }
        
        internal static async Task<HostCluster> GetDbList (MessageContext context) {
            var authorizedDatabases = Helper.CreateHashSet(4, DatabaseFilterComparer.Instance);
            var taskAuthorizer      = context.syncContext.authState.taskAuthorizer;
            taskAuthorizer.AddAuthorizedDatabases(authorizedDatabases);
            var hub             = context.Hub;
            var databases       = hub.GetDatabases();
            var databaseList    = new List<DbContainers>(databases.Count);
            foreach (var pair in databases) {
                var database    = pair.Value;
                if (!DatabaseFilter.IsAuthorizedDatabase(authorizedDatabases, database.nameShort))
                    continue;
                var dbContainers    = await database.GetDbContainers(database.name, hub).ConfigureAwait(false);
                databaseList.Add(dbContainers);
            }
            return new HostCluster{ databases = databaseList };
        }
    }
}
