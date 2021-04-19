// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.EntityGraph.Database
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public class SyncRequest
    {
        public  List<DbCommand>                         commands;
    }
    
    public partial class SyncResponse
    {
        public  List<DbCommandResult>                   results;
        public  Dictionary<string, ContainerEntities>   containerResults;
    }
    
    // ------ ContainerEntities
    public partial class ContainerEntities
    {
        public  string                          container; // only for debugging
        public  Dictionary<string, EntityValue> entities;
    }
    
    // ------------------------------ DatabaseCommand ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntities),           Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    public abstract class DbCommand
    {
        internal abstract DbCommandResult   Execute(EntityDatabase database, SyncResponse response);
        internal abstract CommandType       CommandType { get; }
    }
    
    // ------------------------------ CommandResult ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    public abstract class DbCommandResult
    {
        internal abstract CommandType       CommandType { get; }
    }
    
    public enum CommandType
    {
        Read,
        Query,
        Create,
        Patch
    }
    
    // --------------------------------------- CreateEntities ---------------------------------------
    public partial class CreateEntities : DbCommand
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
    }
    
    public partial class CreateEntitiesResult : DbCommandResult
    {
    }

    // --------------------------------------- ReadEntities ---------------------------------------
    public partial class ReadEntities : DbCommand
    {
        public  string                      container;
        public  List<string>                ids;
        public  List<ReadReference>         references;
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public partial class ReadEntitiesResult : DbCommandResult
    {
        public  List<ReadReferenceResult>   references;
    }
    
    // ---
    public class ReadReference
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string                  refPath; // e.g. ".items[*].article"
        public  string                  container;
        public  List<string>            ids;
    }
    
    public class ReadReferenceResult
    {
        public  string                  container;
        public  List<string>            ids;
    }
    
    // --------------------------------------- QueryEntities ---------------------------------------
    public partial class QueryEntities : DbCommand
    {
        public  string                      container;
        public  FilterOperation             filter;
        public  List<QueryReference>        references;
    }
    
    public partial class QueryEntitiesResult : DbCommandResult
    {
        public  List<string>                ids;
        public  List<QueryReferenceResult>  references;
    }
    
    // ---
    /// In contrast to <see cref="ReadReference"/> which know the ids of referenced entities in advance
    /// a <see cref="QueryReference"/> doesnt know the ids of referenced  entities when initiating a query.
    /// The ids are only available as a result after <see cref="QueryEntities"/> is executed.   
    public class QueryReference
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string                  refPath; // e.g. ".items[*].article"
        public  string                  container;
    }
    
    public class QueryReferenceResult
    {
        public  string                  container;
        public  List<string>            ids;
    }
    
    // --------------------------------------- PatchEntities ---------------------------------------
    public partial class PatchEntities : DbCommand
    {
        public  string              container;
        public  List<EntityPatch>   entityPatches;
    }

    public class EntityPatch
    {
        public string               id;
        public List<JsonPatch>      patches;
    }

    public partial class PatchEntitiesResult : DbCommandResult
    {
    }
}
