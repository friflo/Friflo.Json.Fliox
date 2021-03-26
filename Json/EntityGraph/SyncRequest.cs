using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Database;
using Friflo.Json.Mapper;

namespace Friflo.Json.EntityGraph
{
    public class SyncRequest
    {
        public List<DatabaseCommand> commands;

        public void Execute(EntityDatabase database) {
            foreach (var command in commands) {
                command.Execute(database);
            }
        }
    }

    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntities),  Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),    Discriminant = "read")]
    public abstract class DatabaseCommand
    {
        public abstract void        Execute(EntityDatabase database);
        public abstract CommandType CommandType { get; }
    }
    
    public class CreateEntities : DatabaseCommand
    {
        public  string              containerName;
        public  List<KeyValue>      entities;

        public override CommandType CommandType => CommandType.Create;

        public override void Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            container.CreateEntities(entities);   
        }
    }
    
    public class ReadEntities : DatabaseCommand
    {
        public  string              containerName;
        public  List<string>        ids;
        
        [Fri.Ignore]
        public  List<KeyValue>      entitiesResult;

        
        public override CommandType CommandType => CommandType.Read;
        
        public override void Execute(EntityDatabase database) {
            var container = database.GetContainer(containerName);
            entitiesResult = container.ReadEntities(ids).ToList(); 
        }
    }

    public enum CommandType
    {
        Read,
        Create
    }
}
