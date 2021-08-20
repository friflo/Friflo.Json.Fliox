// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class EntityIdStore : EntityStore {
        public  readonly    EntitySet <Guid,    GuidEntity>      guidEntities;
        public  readonly    EntitySet <int,     IntEntity>       intEntities;
        public  readonly    EntitySet <long,    LongEntity>      longEntities;
        public  readonly    EntitySet <short,   ShortEntity>     shortEntities;
        public  readonly    EntitySet <string,  CustomIdEntity>  customIdEntities;
        public  readonly    EntitySet <string,  EntityRefs>      entityRefs;
#if !UNITY_5_3_OR_NEWER
        public  readonly    EntitySet <string,  CustomIdEntity2> customIdEntities2;
#endif

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities      = new EntitySet <Guid,    GuidEntity>      (this);
            intEntities       = new EntitySet <int,     IntEntity>       (this);
            longEntities      = new EntitySet <long,    LongEntity>      (this);
            shortEntities     = new EntitySet <short,   ShortEntity>     (this);
            customIdEntities  = new EntitySet <string,  CustomIdEntity>  (this);
            entityRefs        = new EntitySet <string,  EntityRefs>      (this);
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
    
    public class EntityRefs {
        
        [Fri.Required]  public string                       id;
                        public Ref <Guid,   GuidEntity>     guidEntity;
                        public Ref <int,    IntEntity>      intEntity;
                        public Ref <long,   LongEntity>     longEntity;
                        public Ref <short,  ShortEntity>    shortEntity;
                        public Ref <string, CustomIdEntity> customIdEntity;
                        
                        public List<Ref <Guid, GuidEntity>> guidEntities;
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