// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.DB.NoSQL
{
    // --- models
    public class _ReservedKeys {
        public  Guid    id;     // secret to ensure the client has reserved the keys
        [Fri.Required]  public  int     start;
        [Fri.Required]  public  int     count;
        public  string  user;   // to track back who reserved key in case of abusing it
    }
    
    public class _Sequence {
        [Fri.Key]       public  string  container;
        [Fri.Required]  public  int     autoId;
        [Fri.Required]  public  string  _etag;
    }

    public class SequenceStore : EntityStore
    {
        public  EntitySet <Guid,    _ReservedKeys>   reservedKeys;
        public  EntitySet <string,  _Sequence>       sequence;
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId)
            : base(database, typeStore, clientId) { }
    }

    // --- protocol
    public class AutoIncrement {
        [Fri.Required]  public  int     count;
    }
    
    public class AutoIncrementResult {
        [Fri.Required]  public  int     start;
        [Fri.Required]  public  int     count;
    }
}