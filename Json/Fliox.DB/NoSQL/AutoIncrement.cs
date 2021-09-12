// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public class AutoIncrement {
        public  int         count;
    }
    
    public class AutoIncrementResult {
        public  List<long>  ids;
    }
    
    public class SequenceStore : EntityStore
    {
        public  EntitySet <string, AutoIncSequence>    sequence; 
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId)
            : base(database, typeStore, clientId) { }
    }
    
    public class AutoIncSequence {
        [Key]
        public  string  container;
        public  long    autoId;
        [Fri.Property(Name = "_etag")]
        public  string  etag;
    }
}