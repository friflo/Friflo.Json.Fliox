// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Mapper;

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
    [Fri.Polymorph(typeof(CreateEntities),  Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),    Discriminant = "read")]
    public abstract class DatabaseCommand
    {
        public abstract CommandResult  Execute(EntityDatabase database);
        public abstract CommandType     CommandType { get; }
    }
    
    // ------------------------------ CommandResult ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),  Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),    Discriminant = "read")]
    public abstract class CommandResult
    {
        public abstract CommandType CommandType { get; }
    }
    
    public enum CommandType
    {
        Read,
        Create
    }
    
    // ------ CreateEntities
    public class CreateEntities : DatabaseCommand
    {
        public  string              containerName;
        public  List<KeyValue>      entities;

        public override CommandType CommandType => CommandType.Create;
        public override string      ToString() => containerName;

        public override CommandResult Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
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
        public override string      ToString() => containerName;
        
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
}
