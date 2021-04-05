// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Database
{
    public class SyncRequest
    {
        public List<DatabaseCommand> commands;
    }
    
    public class SyncResponse
    {
        public  List<CommandResult>                     results;
        public  Dictionary<string, SyncDependencies>    syncDependencies;

        public SyncDependencies GetSyncDependencies(string container) {
            if (syncDependencies.TryGetValue(container, out SyncDependencies syncDep))
                return syncDep;
            syncDep = new SyncDependencies {
                container = container,
                entities = new Dictionary<string,EntityValue>()
            };
            syncDependencies.Add(container, syncDep);
            return syncDep;
        }
    }
    
    // ------------------------------ DatabaseCommand ------------------------------
    
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    public abstract class DatabaseCommand
    {
        public abstract CommandResult   Execute(EntityDatabase database, SyncResponse response);
        public abstract CommandType     CommandType { get; }
    }
    
    // ------------------------------ CommandResult ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    public abstract class CommandResult
    {
        public abstract CommandType CommandType { get; }
    }
    
    public enum CommandType
    {
        Read,
        Create,
        Patch
    }
    
    // ------ CreateEntities
    public class CreateEntities : DatabaseCommand
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;

        public override CommandType CommandType => CommandType.Create;
        public override string      ToString() => "container: " + container;

        public override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.value.json = patcher.Copy(entity.Value.value.json, true);
                }
            }
            entityContainer.CreateEntities(entities);
            return new CreateEntitiesResult();
        }
    }
    
    public class CreateEntitiesResult : CommandResult
    {
        public override CommandType CommandType => CommandType.Create;
    }

    // ------ SyncDependencies
    public class SyncDependencies : CommandResult
    {
        public  string                          container; // only for debugging
        public  Dictionary<string, EntityValue> entities;

        public void AddEntities(Dictionary<string, EntityValue> add) {
            foreach (var entity in add) {
                entities.TryAdd(entity.Key, entity.Value);
            }
        }

        public override CommandType CommandType => CommandType.Read;
    }
    
    // ------ ReadEntities
    public class ReadEntities : DatabaseCommand
    {
        public  string                  container;
        public  List<string>            ids;
        public  List<ReadDependency>    dependencies;
        
        public override CommandType CommandType => CommandType.Read;
        public override string      ToString() => "container: " + container;
        
        public override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            var entities = entityContainer.ReadEntities(ids);
            var syncDeps = response.GetSyncDependencies(container);
            syncDeps.AddEntities(entities);
            var dependencyResults = entityContainer.ReadDependencies(dependencies, entities, response);
            var result = new ReadEntitiesResult {
                dependencies    = dependencyResults
            };
            return result; 
        }
    }
    
    /// The data of requested entities are added to <see cref="SyncDependencies.entities"/> 
    public class ReadEntitiesResult : CommandResult
    {
        public  List<ReadDependencyResult>  dependencies;

        public override CommandType CommandType => CommandType.Read;
    }
    
    // ------ ReadDependency
    public class ReadDependency
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These dependent entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string          refPath; // e.g. ".items[*].article"
        public  string          container;
        public  List<string>    ids;
    }
    
    public class ReadDependencyResult
    {
        public  string          container;
        public  List<string>    ids;
    }
    
    // ------ PatchEntities
    public class PatchEntities : DatabaseCommand
    {
        public  string              container;
        public  List<EntityPatch>   entityPatches;
        
        public override CommandType CommandType => CommandType.Patch;
        public override string      ToString() => "container: " + container;
        
        public override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetContainer(container);
            entityContainer.PatchEntities(entityPatches);
            return new PatchEntitiesResult(); 
        }
    }

    public class EntityPatch
    {
        public string       id;
        public List<Patch>  patches;
    }

    public class PatchEntitiesResult : CommandResult
    {
        public override CommandType CommandType => CommandType.Patch;
    }
}
