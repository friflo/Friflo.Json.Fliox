// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public class AutoIncrement {
        [Fri.Required]
        public  int         count;
    }
    
    public class AutoIncrementResult {
        [Fri.Required]
        public  List<long>  ids;
    }
    
    public class SequenceStore : EntityStore
    {
        public  EntitySet <string, AutoIncSequence>    sequence; 
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId)
            : base(database, typeStore, clientId) { }
    }
    
    public class AutoIncSequence {
        [Fri.Key]
        public  string  container;
        [Fri.Required]
        public  long    autoId;
        [Fri.Required]
        [Fri.Property(Name = "_etag")]
        public  string  etag;
    }
}