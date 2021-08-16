// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public class TestEntityId
    {
        [UnityTest] public IEnumerator GuidIdCoroutine() { yield return RunAsync.Await(AssertGuidId(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  GuidIdAsync() { await AssertGuidId(); }
        
        private static async Task AssertGuidId() {
            using (var typeStore    = new TypeStore())
            using (var database     = new MemoryDatabase())
            using (var guidStore    = new GuidStore(database, typeStore, "guidStore")) {
                const string id = "87db6552-a99d-4d53-9b20-8cc797db2b8f";
                var entity      = new GuidEntity { id = new Guid(id)};
                var create      = guidStore.guidEntities.Create(entity);
                
                await guidStore.Sync();
                
                IsTrue(create.Success);
                
                var read = guidStore.guidEntities.Read();
                var find = read.Find(id);
                    
                await guidStore.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
            }
        }
    }
    
    public class GuidStore : EntityStore {
        public readonly EntitySet<GuidEntity> guidEntities;

        public GuidStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {
            guidEntities = new EntitySet<GuidEntity>(this);
        }
    }

    public class GuidEntity {
        public Guid id;
    }
}