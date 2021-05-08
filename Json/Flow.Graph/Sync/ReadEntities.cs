// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public class ReadEntities
    {
        public  HashSet<string>                 ids;
        public  List<References>                references;
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