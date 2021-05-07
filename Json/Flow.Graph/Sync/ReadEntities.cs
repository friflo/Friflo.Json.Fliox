// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class ReadEntities
    {
        public  HashSet<string>                 ids;
        public  List<References>                references;
        
        internal async Task ReadReferences(ReadEntitiesResult readResult, EntityContainer entityContainer, SyncResponse response) {
            List<ReferencesResult> readRefResults = null;
            if (references != null && references.Count > 0) {
                readRefResults = await entityContainer.ReadReferences(references, readResult.entities, entityContainer.name, response);
            }
            readResult.references = readRefResults;
        }
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public class ReadEntitiesResult: ICommandResult
    {
        public  List<ReferencesResult>          references;
        public  CommandError                    Error { get; set; }

        [Fri.Ignore]
        public  Dictionary<string,EntityValue>  entities;
    }
}