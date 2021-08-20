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
        public  readonly    EntitySet <string, GuidEntity>      guidEntities;
        public  readonly    EntitySet <string, IntEntity>       intEntities;
        public  readonly    EntitySet <string, LongEntity>      longEntities;
        public  readonly    EntitySet <string, ShortEntity>     shortEntities;
        public  readonly    EntitySet <string, CustomIdEntity>  customIdEntities;
#if !UNITY_5_3_OR_NEWER
        public  readonly    EntitySet <string, CustomIdEntity2> customIdEntities2;
#endif

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities       = new EntitySet <string, GuidEntity>      (this);
            intEntities        = new EntitySet <string, IntEntity>       (this);
            longEntities       = new EntitySet <string, LongEntity>      (this);
            shortEntities      = new EntitySet <string, ShortEntity>     (this);
            customIdEntities   = new EntitySet <string, CustomIdEntity>  (this);
#if !UNITY_5_3_OR_NEWER
            customIdEntities2  = new EntitySet <string, CustomIdEntity2> (this);
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