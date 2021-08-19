// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class EntityIdStore : EntityStore {
        public  readonly    EntitySet<GuidEntity,       string>  guidEntities;
        public  readonly    EntitySet<IntEntity,        string>  intEntities;
        public  readonly    EntitySet<LongEntity,       string>  longEntities;
        public  readonly    EntitySet<ShortEntity,      string>  shortEntities;
        public  readonly    EntitySet<CustomIdEntity,   string>  customIdEntities;
#if !UNITY_5_3_OR_NEWER
        public  readonly    EntitySet<CustomIdEntity2,  string>  customIdEntities2;
#endif

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities       = new EntitySet<GuidEntity,      string> (this);
            intEntities        = new EntitySet<IntEntity,       string> (this);
            longEntities       = new EntitySet<LongEntity,      string> (this);
            shortEntities      = new EntitySet<ShortEntity,     string> (this);
            customIdEntities   = new EntitySet<CustomIdEntity,  string> (this);
#if !UNITY_5_3_OR_NEWER
            customIdEntities2  = new EntitySet<CustomIdEntity2, string> (this);
#endif
        }
    }

    public class GuidEntity {
        public Guid id;
    }
    
    public class IntEntity {
        public int  id;
    }
    
    public class LongEntity {
        public long Id { get; set; }
    }
    
    public class ShortEntity {
        public short id;
    }
    
    public class CustomIdEntity {
        [Fri.Key]
        [Fri.Required]  public string customId;
    }
    
#if !UNITY_5_3_OR_NEWER
    // Apply [Key]      alternatively by System.ComponentModel.DataAnnotations.KeyAttribute
    // Apply [Required] alternatively by System.ComponentModel.DataAnnotations.RequiredAttribute
    public class CustomIdEntity2 {
        [Key] 
        [Required]  public string customId2;
    }
#endif
}