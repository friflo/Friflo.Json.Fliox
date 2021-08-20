// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
        [UnityTest] public IEnumerator EntityIdCoroutine() { yield return RunAsync.Await(AssertEntityId(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  EntityIdAsync() { await AssertEntityId(); }
        
        private static async Task AssertEntityId() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var typeStore    = new TypeStore())
            using (var database     = new FileDatabase(CommonUtils.GetBasePath() + "assets/Graph/EntityIdStore")) {
                var entityRef = new EntityRefs { id = "entity-ref-1" };
                // --- Guid as entity id ---
                var guidId = new Guid("87db6552-a99d-4d53-9b20-8cc797db2b8f");
                // Test: EntityId<T>.GetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var entity  = new GuidEntity { id = guidId};
                    var create  = store.guidEntities.Update(entity);
                    
                    await store.Sync();
                    
                    IsTrue(create.Success);
                    
                    var read = store.guidEntities.Read();
                    var find = read.Find(guidId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    IsTrue(entity == find.Result);
                    entityRef.guidEntity = entity;
                    entityRef.guidEntities = new List<Ref<Guid, GuidEntity>> { entity };
                }
                // Test: EntityId<T>.SetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var read = store.guidEntities.Read();
                    var find = read.Find(guidId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    AreEqual(guidId, find.Result.id);
                }
                
                // --- int as entity id ---
                const int intId = 1234567890;
                // Test: EntityId<T>.GetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var entity  = new IntEntity { id = intId};
                    var create  = store.intEntities.Update(entity);
                    
                    await store.Sync();
                    
                    IsTrue(create.Success);
                    
                    var read = store.intEntities.Read();
                    var find = read.Find(intId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    IsTrue(entity == find.Result);
                    entityRef.intEntity = entity;
                }
                // Test: EntityId<T>.SetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var read = store.intEntities.Read();
                    var find = read.Find(intId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    AreEqual(intId, find.Result.id);
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
                    var find = read.Find(longId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    IsTrue(entity == find.Result);
                    entityRef.longEntity = entity;
                }
                // Test: EntityId<T>.SetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var read = store.longEntities.Read();
                    var find = read.Find(longId.ToString());
                        
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
                    var find = read.Find(shortId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    IsTrue(entity == find.Result);
                    entityRef.shortEntity = entity;
                }
                // Test: EntityId<T>.SetEntityId()
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var read = store.shortEntities.Read();
                    var find = read.Find(shortId.ToString());
                        
                    await store.Sync();
                    
                    IsTrue(find.Success);
                    AreEqual(shortId, find.Result.id);
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
                    var create = store.entityRefs.Create(entityRef);
                    
                    await store.Sync();
                    
                    IsTrue(create.Success);
                }
                using (var store    = new EntityIdStore(database, typeStore, "guidStore")) {
                    var read = store.entityRefs.Read();
                    
                    var find = read.Find(entityRef.id);
                    read.ReadRef(er => er.guidEntity);
                    read.ReadRef(er => er.intEntity);
                    read.ReadRef(er => er.longEntity);
                    read.ReadRef(er => er.shortEntity);
                    read.ReadRef(er => er.customIdEntity);
                    
                    read.ReadArrayRefs(er => er.guidEntities);
     
                        
                    await store.Sync();                   
                   
                    IsTrue(find.Success);
                    var result = find.Result;
                    AreEqual(entityRef.id, result.id);
                    IsNotNull(result.guidEntity);
                    IsNotNull(result.intEntity);
                    IsNotNull(result.longEntity);
                    IsNotNull(result.shortEntity);
                    IsNotNull(result.customIdEntity);
                }
#if !UNITY_5_3_OR_NEWER
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
#endif
            }
        }
    }
}