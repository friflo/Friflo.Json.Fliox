// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public class AutoIncrement {
        public  int count;
    }
    
    public class AutoIncrementResult {
    }
    
    public class SequenceStore : EntityStore
    {
        public  EntitySet <string, Sequence>    sequence; 
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
        }
    }
    
    public class Sequence {
        [Key]
        public  string  container;
        public  int     autoId;
        [Fri.Property(Name = "_etag")]
        public  int     etag;
    }
}