// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
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
    public class TestEntityKey
    {
        [UnityTest] public IEnumerator EntityKeyCoroutine() { yield return RunAsync.Await(AssertEntityKey(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityKeyAsync() { await AssertEntityKey(); }
        
        private static async Task AssertEntityKey() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/EntityIdStore")) {
                await AssertEntityKeyTests (database, typeStore);
            }
        }
        
        [UnityTest] public IEnumerator EntityKeyCoroutineLoopback() { yield return RunAsync.Await(AssertEntityKeyLoopback(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityKeyAsyncLoopback() { await AssertEntityKeyLoopback(); }
        
        private static async Task AssertEntityKeyLoopback() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/EntityIdStore"))
            using (var database     = new LoopbackDatabase(fileDatabase))
            {
                await AssertEntityKeyTests (database, typeStore);
            }
        }

        private static async Task AssertEntityKeyTests(EntityDatabase database, TypeStore typeStore) {
            var entityRef = new EntityRefs { id = "entity-ref-1" };
            
            // --- int as entity id ---
            const int intId  = 1234567890;
            const int intId2 = 1222222222;
            var intEntity  = new IntEntity { id = intId };
            var intEntity2 = new IntEntity { id = intId2 };
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var create  = store.intEntities.Upsert(intEntity);
                var create2 = store.intEntities.Upsert(intEntity2);
                
                await store.Sync();
                
                IsTrue(create.Success);
                IsTrue(create2.Success);
                
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(intEntity == find.Result);
                entityRef.intEntity         = intEntity;
                entityRef.intNullEntity     = default;
                entityRef.intNullEntity2    = intEntity2;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(intId, find.Result.id);
            }
            
            // --- Guid as entity id ---
            var guidId  = new Guid("11111111-1111-1111-1111-111111111111");
            var guidId2 = new Guid("22222222-2222-2222-2222-222222222222");
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new GuidEntity { id = guidId};
                var create  = store.guidEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.guidEntity        = entity;
                entityRef.intEntities       = new List<Ref<int, IntEntity>> { intEntity };
                entityRef.intNullEntities   = new List<Ref<int?, IntEntity>> { intEntity, default };
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(guidId, find.Result.id);
            }
            
            // --- Guid? as entity id ---
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new GuidEntity { id = guidId2 };
                var create  = store.guidEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.guidEntities.Read();
                var find = read.Find(guidId2);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.guidNullEntity = entity;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.guidEntities.Read();
                var find = read.Find(guidId2);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(guidId2, find.Result.id);
            }
            

            
            // --- long as entity id ---
            const long longId = 1234567890123456789;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new LongEntity { Id = longId};
                var create  = store.longEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.longEntity        = entity;
                entityRef.longNullEntity    = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(longId, find.Result.Id);
            }
            
            // --- short as entity id ---
            const short shortId = 12345;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new ShortEntity { id = shortId };
                var create  = store.shortEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.shortEntity       = entity;
                entityRef.shortNullEntity   = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(shortId, find.Result.id);
            }
            
            // --- byte as entity id ---
            const byte byteId = 123;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new ByteEntity { id = byteId };
                var create  = store.byteEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.byteEntity        = entity;
                entityRef.byteNullEntity    = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(byteId, find.Result.id);
            }
            
            // --- string as custom entity id ---
            const string stringId = "abc";
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new CustomIdEntity { customId = stringId};
                var create  = store.customIdEntities.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.customIdEntity = entity;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                AreEqual(stringId, find.Result.customId);
            }
            
            // --- write and read Ref<>'s
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var create = store.entityRefs.Upsert(entityRef);
                
                await store.Sync();
                
                IsTrue(create.Success);
            }
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var read = store.entityRefs.Read();
                
                var find = read.Find(entityRef.id);
                var guidRef      = read.ReadRef        (er => er.guidEntity);
                var guidNullRef  = read.ReadRef        (er => er.guidNullEntity);
                
                var intRef       = read.ReadRef        (er => er.intEntity);
                var intNullRef   = read.ReadRef        (er => er.intNullEntity);
                var intNullRef2  = read.ReadRef        (er => er.intNullEntity2);
                
                var longRef      = read.ReadRef        (er => er.longEntity);
                var longNullRef  = read.ReadRef        (er => er.longNullEntity);
                
                var shortRef     = read.ReadRef        (er => er.shortEntity);
                var shortNullRef = read.ReadRef        (er => er.shortNullEntity);
                
                var byteRef      = read.ReadRef        (er => er.byteEntity);
                var byteNullRef  = read.ReadRef        (er => er.byteNullEntity);
                
                var customIdRef  = read.ReadRef        (er => er.customIdEntity);
                
                var intRefs      = read.ReadArrayRefs  (er => er.intEntities);
                var intNullRefs  = read.ReadArrayRefs  (er => er.intNullEntities);

                await store.Sync();                   
               
                IsTrue(find.Success);
                var result = find.Result;
                AreEqual(entityRef.id, result.id);
                IsNotNull(result.guidEntity.Entity);
                IsNotNull(result.guidNullEntity.Entity);
                
                IsNotNull(result.intEntity.Entity);
                IsNull   (result.intNullEntity.Entity);
                IsTrue   (result.intNullEntity.TryEntity(out _));
                IsNotNull(result.intNullEntity2.Entity);
                
                IsNotNull(result.longEntity.Entity);
                IsNull   (result.longNullEntity.Entity);
                
                IsNotNull(result.shortEntity.Entity);
                IsNull   (result.shortNullEntity.Entity);
                
                IsNotNull(result.byteEntity.Entity);
                IsNull   (result.byteNullEntity.Entity);
                
                IsNotNull(result.customIdEntity.Entity);
                IsNotNull(result.intEntities[0].Entity);
                
                IsNotNull(guidRef.Result);
                IsTrue(guidId   ==  guidRef.Key);
                IsTrue(guidId2  ==  guidNullRef.Key);
                
                IsTrue(intId    ==  intRef.Key);
                IsNull(             intNullRef.Result);
                IsTrue(intId2   ==  intNullRef2.Key);
                
                IsTrue(longId   ==  longRef.Key);
                IsNull(             longNullRef.Result);
                
                IsTrue(shortId  ==  shortRef.Key);
                IsNull(             shortNullRef.Result);
                
                IsTrue(byteId   ==  byteRef.Key);
                IsNull(             byteNullRef.Result);
                
                IsTrue(stringId ==  customIdRef.Key);
                
                AreEqual (1,        intRefs.Results.Count);
                IsNotNull(          intRefs.Results[intId]);
                IsNotNull(          intRefs[intId]);
                
                AreEqual (1,        intNullRefs.Results.Count);
                IsNotNull(          intNullRefs.Results[intId]);
                IsNotNull(          intNullRefs[intId]);
            }
            
            // ensure QueryTask<> results enables type-safe key access
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var guidEntities    = store.guidEntities.   QueryAll();
                var intEntities     = store.intEntities.    QueryAll();
                var longEntities    = store.longEntities.   QueryAll();
                var shortEntities   = store.shortEntities.  QueryAll();

                await store.Sync();
               
                IsNotNull(guidEntities  [guidId]);
                IsNotNull(guidEntities  [guidId2]);
                IsNotNull(intEntities   [intId]);
                IsNotNull(longEntities  [longId]);
                IsNotNull(shortEntities [shortId]);
            }
            
            // --- string as custom entity id ---
            const string stringId2 = "xyz";
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                var entity  = new CustomIdEntity2 { customId2 = stringId2};
                var create  = store.customIdEntities2.Upsert(entity);
                
                await store.Sync();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities2.Read();
                var find = read.Find(stringId2);
                    
                await store.Sync();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
            }
            // Test: EntityKeyT<TKey,T>.SetId()
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