// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestRefAssignment () { await TestUse(async (store) => await AssertRefAssignment   (store)); }
        
        private static async Task AssertRefAssignment(PocStore store) {
            var articles    = store.articles;
            var producers   = store.producers;
            
            // --- assign id to reference
            var newArticle1 = new Article { producer = "unknown-producer-1" };
            Exception e = Throws<UnresolvedRefException>(() => { var _ = newArticle1.producer.Entity; });
            AreEqual("Accessed unresolved reference. Ref<Producer> (id: 'unknown-producer-1')", e.Message);
            
            
            // --- assign entity instance to reference
            var producer    = new Producer { id = "unknown-producer-2" }; // producer will not synced (implicit nor explicit) to database
            var newArticle2 = new Article { producer = producer };
            newArticle2.producer = producer;
            IsTrue(producer == newArticle2.producer.Entity);
            

            // --- read entity with an unresolved reference (galaxy.producer) from database
            var readArticles    = articles.Read();
            var galaxyTask      = readArticles.Find("article-galaxy"); // entity exist in database 
            await store.Sync();  // -------- Sync --------

            var galaxy = galaxyTask.Result;
            // the referenced entity "producer-samsung" is not resolved until now.
            e = Throws<UnresolvedRefException>(() => { var _ = galaxy.producer.Entity; });
            AreEqual("Accessed unresolved reference. Ref<Producer> (id: 'producer-samsung')", e.Message);
            IsFalse(galaxy.producer.TryEntity(out Producer result));
            IsNull(result);
            
            
            // --- resolve reference (galaxy.producer) from database
            var readProducers = producers.Read();
            galaxy.producer.FindBy(readProducers);                                  // schedule resolving reference
            var newArticle1Producer = newArticle1.producer.FindBy(readProducers);   // schedule resolving reference
            var newArticle2Producer = newArticle2.producer.FindBy(readProducers);   // schedule resolving reference
            
            // assign producer field with id "producer-apple". Note: iphone is never synced
            var iphone = new Article  { name = "iPhone 11", producer = "producer-apple" };
            var findProducer = iphone.producer.FindBy(readProducers); // resolve referenced Producer
            
            var tesla  = new Producer { id = "producer-tesla", name = "Tesla" };
            // assign producer field with entity instance tesla
            var model3 = new Article  { id = "article-model3", name = "Model 3", producer = tesla };
            IsTrue(model3.producer.TryEntity(out result));
            AreSame(tesla, result);
            
            AreEqual("Tesla",   model3.producer.Entity.name);   // Entity is directly accessible

            await store.Sync();  // -------- Sync --------
            
            IsNull  (newArticle1Producer.Result);
            IsNull  (newArticle1.producer.Entity);
            
            IsNull  (newArticle2Producer.Result);
            IsNull  (newArticle2.producer.Entity);
            
            AreEqual("Apple",   findProducer.Result.name);
            AreEqual("Samsung", galaxy.producer.Entity.name);   // after Sync() Entity is accessible
            AreEqual("Apple",   iphone.producer.Entity.name);   // after Sync() Entity is accessible
        }
    }
}