// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestEntityIdStore : EntityStore {
        public readonly EntitySet<GuidEntity> guidEntities;

        public TestEntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities = new EntitySet<GuidEntity>(this);
        }
    }

    public class GuidEntity {
        public Guid id;
    }
}