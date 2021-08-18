// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class EntityIdStore : EntityStore {
        public  readonly    EntitySet<GuidEntity>       guidEntities;
        public  readonly    EntitySet<IntEntity>        intEntities;
        public  readonly    EntitySet<LongEntity>       longEntities;
        public  readonly    EntitySet<CustomIdEntity>   customIdEntities;

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities        = new EntitySet<GuidEntity>     (this);
            intEntities         = new EntitySet<IntEntity>      (this);
            longEntities        = new EntitySet<LongEntity>     (this);
            customIdEntities    = new EntitySet<CustomIdEntity> (this);
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
    
    public class CustomIdEntity {
        [Fri.Key]
        [Fri.Required]  public string customId;
    }
}