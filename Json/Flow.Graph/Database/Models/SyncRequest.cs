// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database.Models
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public class SyncRequest
    {
        public  List<DatabaseTask>                      tasks;
    }
    
    public partial class SyncResponse
    {
        public  List<TaskResult>                        results;
        public  Dictionary<string, ContainerEntities>   containerResults;
    }
    
    // ------ ContainerEntities
    public partial class ContainerEntities
    {
        public  string                          container; // only for debugging
        public  Dictionary<string, EntityValue> entities;
    }
    
    // ------------------------------ DatabaseCommand ------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(UpdateEntities),          Discriminant = "update")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntities),           Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntities),          Discriminant = "delete")]
    public abstract class DatabaseTask
    {
        internal abstract Task<TaskResult>  Execute(EntityDatabase database, SyncResponse response);
        internal abstract TaskType          TaskType { get; }
    }
    
    // ------------------------------ CommandResult ------------------------------
    [Fri.Discriminator("task")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(UpdateEntitiesResult),    Discriminant = "update")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    [Fri.Polymorph(typeof(DeleteEntitiesResult),    Discriminant = "delete")]
    public abstract class TaskResult
    {
        internal abstract TaskType          TaskType { get; }
    }
    
    public enum TaskType
    {
        Read,
        Query,
        Create,
        Update,
        Patch,
        Delete
    }
    
    // --------------------------------------- CreateEntities ---------------------------------------
    public partial class CreateEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
    }
    
    public partial class CreateEntitiesResult : TaskResult
    {
    }
    
    // --------------------------------------- UpdateEntities ---------------------------------------
    public partial class UpdateEntities : DatabaseTask
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
    }
    
    public partial class UpdateEntitiesResult : TaskResult
    {
    }

    // --------------------------------------- ReadEntities ---------------------------------------
    public partial class ReadEntities : DatabaseTask
    {
        public  string                          container;
        public  List<string>                    ids;
        public  List<ReadRef>                   readRefs;
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public partial class ReadEntitiesResult : TaskResult
    {
        public   List<ReadRefResult>            readRefs;
        [Fri.Ignore]
        internal Dictionary<string,EntityValue> entities;
    }

    public class ReadRef
    {
        public  References                      reference;
        public  List<string>                    ids;
    }
    
    public class ReadRefResult
    {
        public  ReferencesResult                reference;
    }

    
    // ---
    public class References
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string                  selector; // e.g. ".items[*].article"
        public  string                  container;
        public  List<References>        references;
    }
    
    public class ReferencesResult
    {
        public  string                  selector;  // only for debugging ergonomics
        public  string                  container; // only for debugging ergonomics
        public  HashSet<string>         ids;
        public  List<ReferencesResult>  references;
    }
    
    
    // --------------------------------------- QueryEntities ---------------------------------------
    public partial class QueryEntities : DatabaseTask
    {
        public  string                          container;
        public  string                          filterLinq;
        public  FilterOperation                 filter;
        public  List<References>                references;
    }
    
    public partial class QueryEntitiesResult : TaskResult
    {
        public  string                          container;  // only for debugging ergonomics
        public  string                          filterLinq;
        public  HashSet<string>                 ids;
        public  List<ReferencesResult>          references;
        [Fri.Ignore]
        internal Dictionary<string,EntityValue> entities;
    }
    
    // --------------------------------------- PatchEntities ---------------------------------------
    public partial class PatchEntities : DatabaseTask
    {
        public  string              container;
        public  List<EntityPatch>   entityPatches;
    }

    public class EntityPatch
    {
        public string               id;
        public List<JsonPatch>      patches;
    }

    public partial class PatchEntitiesResult : TaskResult
    {
    }
    
    // --------------------------------------- DeleteEntities ---------------------------------------
    public partial class DeleteEntities : DatabaseTask
    {
        public  string              container;
        public  List<string>        ids;
    }
    
    public partial class DeleteEntitiesResult : TaskResult
    {
    }
}
