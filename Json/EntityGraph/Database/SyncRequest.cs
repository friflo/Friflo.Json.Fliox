// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
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
        public List<CommandResult> results;
    }
    
    // ------------------------------ DatabaseCommand ------------------------------
    
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    public abstract class DatabaseCommand
    {
        public abstract CommandResult   Execute(EntityDatabase database);
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
        public  string              container;
        public  List<KeyValue>      entities;

        public override CommandType CommandType => CommandType.Create;
        public override string      ToString() => "container: " + container;

        public override CommandResult Execute(EntityDatabase database) {
            var entityContainer = database.GetContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.value.json = patcher.Copy(entity.value.json, true);
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
    
    
    // ------ ReadEntities
    public class ReadEntities : DatabaseCommand
    {
        public  string                  container;
        public  List<string>            ids;
        public  List<ReadDependency>    dependencies;                  
        
        public override CommandType CommandType => CommandType.Read;
        public override string      ToString() => "container: " + container;
        
        public override CommandResult Execute(EntityDatabase database) {
            var entityContainer = database.GetContainer(container);
            var entities = entityContainer.ReadEntities(ids).ToList();
            var dependencyResults = entityContainer.ReadDependencies(dependencies, entities);
            var result = new ReadEntitiesResult {
                entities = entities,
                dependencies = dependencyResults
            };
            return result; 
        }
    }

    public class ReadEntitiesResult : CommandResult
    {
        public  List<KeyValue>              entities;
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
        public  string          refPath; // e.g. ".items[*].article"
        public  string          container;
        public  List<KeyValue>  entities;
    }
    
    // ------ PatchEntities
    public class PatchEntities : DatabaseCommand
    {
        public  string              container;
        public  List<EntityPatch>   entityPatches;
        
        public override CommandType CommandType => CommandType.Patch;
        public override string      ToString() => "container: " + container;
        
        public override CommandResult Execute(EntityDatabase database) {
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
