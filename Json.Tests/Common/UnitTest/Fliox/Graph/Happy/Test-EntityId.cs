// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy
{
    public class TestEntityId
    {
        [UnityTest] public IEnumerator EntityIdCoroutine() { yield return RunAsync.Await(AssertEntityId(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityIdAsync() { await AssertEntityId(); }
        
        private static async Task AssertEntityId() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/EntityIdStore")) {
                await AssertEntityIdTests (database, typeStore);
            }
        }
        
        [UnityTest] public IEnumerator EntityIdCoroutineLoopback() { yield return RunAsync.Await(AssertEntityIdLoopback(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityIdAsyncLoopback() { await AssertEntityIdLoopback(); }
        
        private static async Task AssertEntityIdLoopback() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/EntityIdStore"))
            using (var database     = new LoopbackDatabase(fileDatabase))
            {
                await AssertEntityIdTests (database, typeStore);
            }
        }

        private static async Task AssertEntityIdTests(EntityDatabase database, TypeStore typeStore) {
            var entityRef = new EntityRefs { id = "entity-ref-1" };
            
            // --- int as entity id ---
            const int intId = 1234567890;
            var intEntity  = new IntEntity { id = intId};
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var create  = store.intEntities.Update(intEntity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(intEntity == find.Result);
                entityRef.intEntity = intEntity;
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(intId, find.Result.id);
            }
            
            // --- Guid as entity id ---
            var guidId = new Guid("87db6552-a99d-4d53-9b20-8cc797db2b8f");
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new GuidEntity { id = guidId};
                var create  = store.guidEntities.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.guidEntity = entity;
                entityRef.intEntities = new List<Ref<int, IntEntity>> { intEntity };
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(guidId, find.Result.id);
            }
            

            
            // --- long as entity id ---
            const long longId = 1234567890123456789;
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new LongEntity { Id = longId};
                var create  = store.longEntities.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.longEntity = entity;
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(longId, find.Result.Id);
            }
            
            // --- short as entity id ---
            const short shortId = 12345;
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new ShortEntity { id = shortId };
                var create  = store.shortEntities.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.shortEntity = entity;
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(shortId, find.Result.id);
            }
            
            // --- byte as entity id ---
            const byte byteId = 123;
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new ByteEntity { id = byteId };
                var create  = store.byteEntities.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.byteEntity = entity;
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(byteId, find.Result.id);
            }
            
            // --- string as custom entity id ---
            const string stringId = "abc";
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new CustomIdEntity { customId = stringId};
                var create  = store.customIdEntities.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.customIdEntity = entity;
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(stringId, find.Result.customId);
            }
            
            // --- write and read Ref<>'s
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var create = store.entityRefs.Update(entityRef);
                
                await store.Sync();
                
                IsTrue(create.Success);
            }
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.entityRefs.Read();
                
                var find = read.Find(entityRef.id);
                var guidRef      = read.ReadRef        (er => er.guidEntity);
                var intRef       = read.ReadRef        (er => er.intEntity);
                var longRef      = read.ReadRef        (er => er.longEntity);
                var shortRef     = read.ReadRef        (er => er.shortEntity);
                var byteRef      = read.ReadRef        (er => er.byteEntity);
                var customIdRef  = read.ReadRef        (er => er.customIdEntity);
                var guidRefs     = read.ReadArrayRefs  (er => er.intEntities);

                await store.Sync();                   
               
                IsTrue(find.Success);
                var result = find.Result;
                AreEqual(entityRef.id, result.id);
                IsNotNull(result.guidEntity);
                IsNotNull(result.intEntity);
                IsNotNull(result.longEntity);
                IsNotNull(result.shortEntity);
                IsNotNull(result.byteEntity);
                IsNotNull(result.customIdEntity);
                IsNotNull(result.intEntities[0].Entity);
                
                IsNotNull(guidRef.Result);
                IsTrue(guidId   == guidRef.Key);
                IsTrue(intId    == intRef.Key);
                IsTrue(longId   == longRef.Key);
                IsTrue(shortId  == shortRef.Key);
                IsTrue(byteId   == byteRef.Key);
                IsTrue(stringId == customIdRef.Key);
                IsNotNull(guidRefs.Results[intId]);
                IsNotNull(guidRefs[intId]);
            }
            
            // ensure QueryTask<> results enables type-safe key access
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var guidEntities    = store.guidEntities.   QueryAll();
                var intEntities     = store.intEntities.    QueryAll();
                var longEntities    = store.longEntities.   QueryAll();
                var shortEntities   = store.shortEntities.  QueryAll();

                await store.Sync();
               
                IsNotNull(guidEntities  [guidId]);
                IsNotNull(intEntities   [intId]);
                IsNotNull(longEntities  [longId]);
                IsNotNull(shortEntities [shortId]);
            }
            
            // --- string as custom entity id ---
            const string stringId2 = "xyz";
            // Test: EntityId<T>.GetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new CustomIdEntity2 { customId2 = stringId2};
                var create  = store.customIdEntities2.Update(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities2.Read();
                var find = read.Find(stringId2);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
            }
            // Test: EntityId<T>.SetEntityId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.customIdEntities2.Read();
                var find = read.Find(stringId2);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(stringId2, find.Result.customId2);
            }
        }
    }
}