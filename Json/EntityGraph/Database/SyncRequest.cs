// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Diff;

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
        public  string              containerName;
        public  List<KeyValue>      entities;

        public override CommandType CommandType => CommandType.Create;
        public override string      ToString() => "container: " + containerName;

        public override CommandResult Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            // may call serializer.WriteTree() always to ensure a valid JSON value
            if (container.Pretty) {
                var patcher = container.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.value.json = patcher.Copy(entity.value.json, true);
                }
            }
            container.CreateEntities(entities);
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
        public  string              containerName;
        public  List<string>        ids;
        
        public override CommandType CommandType => CommandType.Read;
        public override string      ToString() => "container: " + containerName;
        
        public override CommandResult Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            var entities = container.ReadEntities(ids).ToList();
            var result = new ReadEntitiesResult {
                entities = entities
            };
            return result; 
        }
    }

    public class ReadEntitiesResult : CommandResult
    {
        public  List<KeyValue>      entities;
        
        public override CommandType CommandType => CommandType.Read;
    }
    
    // ------ PatchEntities
    public class PatchEntities : DatabaseCommand
    {
        public  string              containerName;
        public  List<EntityPatch>   entityPatch;
        
        public override CommandType CommandType => CommandType.Patch;
        public override string      ToString() => "container: " + containerName;
        
        public override CommandResult Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            var ids = entityPatch.Select(patch => patch.id).ToList();
            // Read entities to be patched
            var entities = container.ReadEntities(ids).ToList();
            
            // Apply patches
            var patcher = container.SyncContext.jsonPatcher;
            int n = 0;
            foreach (var entity in entities) {
                var patch = entityPatch[n++];
                entity.value.json = patcher.ApplyPatches(entity.value.json, patch.patches, container.Pretty);
            }
            // Write patched entities back
            container.CreateEntities(entities); // should be UpdateEntities
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
        public  List<KeyValue>      entities;
        
        public override CommandType CommandType => CommandType.Patch;
    }
}
