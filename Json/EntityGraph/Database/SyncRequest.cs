// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Burst;
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
        public abstract CommandResult   Execute(EntityDatabase database, CommandContext context);
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
        public override string      ToString() => "container: " + containerName;

        public override CommandResult Execute(EntityDatabase database, CommandContext context) {
            var container = database.GetContainer(containerName);
            // may call serializer.WriteTree() always to ensure a valid JSON value
            if (container.Pretty) {
                context.serializer.SetPretty(true);
                foreach (var entity in entities) {
                    using (var json = new Bytes(entity.value.json)) {
                        context.parser.InitParser(json);
                        context.parser.NextEvent();
                        context.serializer.InitSerializer();
                        context.serializer.WriteTree(ref context.parser);
                        entity.value.json = context.serializer.json.ToString();
                    }
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
        
        public override CommandResult Execute(EntityDatabase database, CommandContext context) {
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
    
    // ------------------------------------ CommandContext ------------------------------------
    public class CommandContext : IDisposable
    {

        public              JsonSerializer  serializer;
        public              JsonParser      parser;

        public void Dispose() {
            parser.Dispose();
            serializer.Dispose();
        }
    }
}
