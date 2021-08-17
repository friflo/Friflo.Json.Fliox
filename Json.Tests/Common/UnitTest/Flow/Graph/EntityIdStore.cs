// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class EntityIdStore : EntityStore {
        public  readonly    EntitySet<GuidEntity>   guidEntities;
        public  readonly    EntitySet<IntEntity>    intEntities;

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities    = new EntitySet<GuidEntity>(this);
            intEntities     = new EntitySet<IntEntity> (this);
        }
    }

    public class GuidEntity {
        public Guid id;
    }
    
    public class IntEntity {
        public int  id;
    }
}