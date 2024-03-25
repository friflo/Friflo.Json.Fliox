// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public class TestEntityKey
    {
        [UnityTest] public IEnumerator EntityKeyCoroutine() { yield return RunAsync.Await(AssertEntityKey(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityKeyAsync() { await AssertEntityKey(); }
        
        private static async Task AssertEntityKey() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var env          = new SharedEnv())
            using (var database     = new FileDatabase(TestGlobals.DB, CommonUtils.GetBasePath() + "assets~/DB/entity_id_db"))
            using (var hub          = new FlioxHub(database, env))
            {
                await AssertEntityKeyTests (hub);
            }
        }
        
        [UnityTest] public IEnumerator EntityKeyCoroutineLoopback() { yield return RunAsync.Await(AssertEntityKeyLoopback(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityKeyAsyncLoopback() { await AssertEntityKeyLoopback(); }
        
        private static async Task AssertEntityKeyLoopback() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var env          = new SharedEnv())
            using (var database     = new FileDatabase(TestGlobals.DB, CommonUtils.GetBasePath() + "assets~/DB/entity_id_db"))
            using (var hub          = new FlioxHub(database, env))
            using (var loopbackHub  = new LoopbackHub(hub))
            {
                await AssertEntityKeyTests (loopbackHub);
            }
        }

        public static async Task AssertEntityKeyTests(FlioxHub database) {
            var entityRef = new EntityRefs { id = "entity-ref-1" };
            
            // --- int as entity id ---
            const int intId  = 1234567890;
            const int intId2 = 1222222222;
            var intEntity  = new IntEntity { id = intId };
            var intEntity2 = new IntEntity { id = intId2 };
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore" }) {
                var create  = store.intEntities.Upsert(intEntity);
                var create2 = store.intEntities.Upsert(intEntity2);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                IsTrue(create2.Success);
                
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(intEntity == find.Result);
                entityRef.intEntity         = intEntity.id;
                entityRef.intNullEntity     = default;
                entityRef.intNullEntity2    = intEntity2.id;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore" }) {
                var read = store.intEntities.Read();
                var find = read.Find(intId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(intId, find.Result.id);
            }
            
            // --- Guid as entity id ---
            var guidId  = new Guid("11111111-1111-1111-1111-111111111111");
            var guidId2 = new Guid("22222222-2222-2222-2222-222222222222");
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database){ ClientId = "guidStore"}) {
                var entity  = new GuidEntity { id = guidId};
                var create  = store.guidEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.guidEntity        = entity.id;
                entityRef.intEntities       = new List<int>     { intEntity.id };
                entityRef.intNullEntities   = new List<int?>    { intEntity.id, default };
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.guidEntities.Read();
                var find = read.Find(guidId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(guidId, find.Result.id);
            }
            
            // --- Guid? as entity id ---
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore" }) {
                var entity  = new GuidEntity { id = guidId2 };
                var create  = store.guidEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.guidEntities.Read();
                var find = read.Find(guidId2);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.guidNullEntity = entity.id;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.guidEntities.Read();
                var find = read.Find(guidId2);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(guidId2, find.Result.id);
            }
            

            
            // --- long as entity id ---
            const long longId = 1234567890123456789;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var entity  = new LongEntity { Id = longId};
                var create  = store.longEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.longEntity        = entity.Id;
                entityRef.longNullEntity    = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.longEntities.Read();
                var find = read.Find(longId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(longId, find.Result.Id);
            }
            
            // --- short as entity id ---
            const short shortId = 12345;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var entity  = new ShortEntity { id = shortId };
                var create  = store.shortEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.shortEntity       = entity.id;
                entityRef.shortNullEntity   = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.shortEntities.Read();
                var find = read.Find(shortId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(shortId, find.Result.id);
            }
            
            // --- byte as entity id ---
            const byte byteId = 123;
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var entity  = new ByteEntity { id = byteId };
                var create  = store.byteEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.byteEntity        = entity.id;
                entityRef.byteNullEntity    = default;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.byteEntities.Read();
                var find = read.Find(byteId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(byteId, find.Result.id);
            }
            
            // --- string as custom entity id ---
            const string stringId = "abc";
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var entity  = new CustomIdEntity { customId = stringId};
                var create  = store.customIdEntities.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
                entityRef.customIdEntity = entity.customId;
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.customIdEntities.Read();
                var find = read.Find(stringId);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(stringId, find.Result.customId);
            }
            
            // --- write and read a records using references
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                store.Options.WriteNull = true;
                var create = store.entityRefs.Upsert(entityRef);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
            }
            using (var store    = new EntityIdStore(database) { ClientId ="guidStore"}) {
                var read = store.entityRefs.Read();
                
                var find = read.Find(entityRef.id);
                var guidRef      = read.ReadRelation(store.guidEntities, er => er.guidEntity);
                var guidNullRef  = read.ReadRelation(store.guidEntities, er => er.guidNullEntity);
                
                var intRef       = read.ReadRelation(store.intEntities, er => er.intEntity);
                var intNullRef   = read.ReadRelation(store.intEntities, er => er.intNullEntity);
                var intNullRef2  = read.ReadRelation(store.intEntities, er => er.intNullEntity2);
                
                var longRef      = read.ReadRelation(store.longEntities, er => er.longEntity);
                var longNullRef  = read.ReadRelation(store.longEntities, er => er.longNullEntity);
                
                var shortRef     = read.ReadRelation(store.shortEntities, er => er.shortEntity);
                var shortNullRef = read.ReadRelation(store.shortEntities, er => er.shortNullEntity);
                
                var byteRef      = read.ReadRelation(store.byteEntities, er => er.byteEntity);
                var byteNullRef  = read.ReadRelation(store.byteEntities,er => er.byteNullEntity);
                
                var customIdRef  = read.ReadRelation(store.customIdEntities,er => er.customIdEntity);
                
                var intRefs      = read.ReadRelations(store.intEntities,er => er.intEntities);
                var intNullRefs  = read.ReadRelations(store.intEntities,er => er.intNullEntities);

                await store.SyncTasks();                   
               
                IsTrue(find.Success);
                var result = find.Result;
                AreEqual(entityRef.id, result.id);
                IsNotNull(result.guidEntity);
                IsNotNull(result.guidNullEntity);
                
                IsNotNull(result.intEntity);
                IsNull   (result.intNullEntity);
                IsNotNull(result.intNullEntity2);
                
                IsNotNull(result.longEntity);
                IsNull   (result.longNullEntity);
                
                IsNotNull(result.shortEntity);
                IsNull   (result.shortNullEntity);
                
                IsNotNull(result.byteEntity);
                IsNull   (result.byteNullEntity);
                
                IsNotNull(result.customIdEntity);
                IsNotNull(result.intEntities[0]);
                
                IsNotNull(guidRef.Result);
                
                IsNull(             intNullRef.Result);
                
                IsNull(             longNullRef.Result);
                
                IsNull(             shortNullRef.Result);
                
                IsNull(             byteNullRef.Result);
                
                AreEqual (1,        intRefs.Result.Count);
                IsNotNull(          intRefs.Result.Find(i => i.id == intId));
                
                AreEqual (1,        intNullRefs.Result.Count);
                IsNotNull(          intNullRefs.Result.Find(i => i.id == intId));
            }
            
            // ensure QueryTask<> results enables type-safe key access
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var guidEntities    = store.guidEntities.   QueryAll();
                var intEntities     = store.intEntities.    QueryAll();
                var longEntities    = store.longEntities.   QueryAll();
                var shortEntities   = store.shortEntities.  QueryAll();

                await store.SyncTasks();
               
                IsNotNull(guidEntities.Result.Find  (i => i.id == guidId));
                IsNotNull(guidEntities.Result.Find  (i => i.id == guidId2));
                IsNotNull(intEntities.Result.Find   (i => i.id == intId));
                IsNotNull(longEntities.Result.Find  (i => i.Id == longId));
                IsNotNull(shortEntities.Result.Find (i => i.id == shortId));
            }
            
            // --- string as custom entity id ---
            const string stringId2 = "xyz";
            // Test: EntityKeyT<TKey,T>.GetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var entity  = new CustomIdEntity2 { customId2 = stringId2};
                var create  = store.customIdEntities2.Upsert(entity);
                
                await store.SyncTasks();
                
                IsTrue(create.Success);
                
                var read = store.customIdEntities2.Read();
                var find = read.Find(stringId2);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                IsTrue(entity == find.Result);
            }
            // Test: EntityKeyT<TKey,T>.SetId()
            using (var store    = new EntityIdStore(database) { ClientId = "guidStore"}) {
                var read = store.customIdEntities2.Read();
                var find = read.Find(stringId2);
                    
                await store.SyncTasks();
                
                IsTrue(find.Success);
                AreEqual(stringId2, find.Result.customId2);
            }
        }
    }
}