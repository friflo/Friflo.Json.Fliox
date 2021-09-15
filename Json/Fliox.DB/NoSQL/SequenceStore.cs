// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedField.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.DB.NoSQL
{
    // --- models
    public class SequenceKeys {
        [Fri.Key]       public  Guid    token;  // secret to ensure the client has reserved the keys
        [Fri.Required]  public  string  container;
        [Fri.Required]  public  long    start;
        [Fri.Required]  public  int     count;
                        public  string  user;   // to track back who reserved keys in case of abuse
    }
    
    public class Sequence {
        [Fri.Key]       public  string  container;
        [Fri.Required]  public  long    autoId;
        [Fri.Property  (Name =        "_etag")]
                        public  string  etag;
    }

    public class SequenceStore : EntityStore
    {
        // public const string ReservedKeys   =   "_ReservedKeys";
        // public const string Sequence       =   "_Sequence";
        
        public readonly EntitySet <Guid,   SequenceKeys>   sequenceKeys;
        public readonly EntitySet <string, Sequence>       sequence;
        
        public  SequenceStore(EntityDatabase database, TypeStore typeStore, string clientId)
            : base(database, typeStore, clientId) { }
    }
}