// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestRefAssignment () { await TestUse(async (store) => await AssertRefAssignment   (store)); }
        
        private static  Task AssertRefAssignment(PocStore store) {
            return Task.CompletedTask;
        }
        
        [Test]
        // ReSharper disable ExpressionIsAlwaysNull
        public void TestRefBasicAssignment () {
            // ReSharper disable once InlineOutVariableDeclaration
            Article result;
            
            // --- assign default Ref<string, Article>
            Ref<string, Article> reference = default;
            IsNull  (reference.Key);
            IsNull  (reference.Entity);
            IsTrue  (reference.TryEntity(out result));
            IsNull  (result);
            
            // all assignments are using the implicit conversion operators from Ref<,>
            
            // --- assign entity reference (Article)
            var article = new Article { id = "some-id" };
            reference = article;
            AreEqual("some-id", reference.Key);
            IsTrue  (article == reference.Entity);
            IsTrue  (reference.TryEntity(out result));
            IsTrue  (article == result);
            
            Article nullArticle = null;
            reference = nullArticle;
            IsNull  (reference.Key);
            IsNull  (reference.Entity);
            IsTrue  (reference.TryEntity(out result));
            IsNull  (result);
            
            var invalidArticle = new Article(); // entity id = null
            var argEx = Throws<ArgumentException>(() => _ = reference = invalidArticle );
            AreEqual("cannot assign entity with Key = null to Ref<String,Article>", argEx.Message);
            
            // --- assign entity key (string)
            string nullKey = null;
            reference = nullKey;
            IsNull  (reference.Key);
            IsNull  (reference.Entity);
            IsTrue  (reference.TryEntity(out result));
            IsNull  (result);
            
            reference = "ref-id";
            AreEqual("ref-id", reference.Key);
            IsFalse (reference.TryEntity(out result));
            IsNull  (result);
            var e = Throws<UnresolvedRefException>(() => _ = reference.Entity);
            AreEqual("Accessed unresolved reference. Ref<Article> (Key: 'ref-id')", e.Message);
            AreEqual("ref-id", e.key);
        }
        
#if !DEBUG && !UNITY_5_3_OR_NEWER
        /// Test boxing behavior of <b>EntityKeyT{TKey,T}.GetKeyAsType{TAsType}</b>/>
        /// Will box in DEBUG - not in RELEASE
        [Test]
        public void TestRefAssignmentNoBoxing () {
            // key (string) is reference type
            var article = new Article { id = "some-id" };
            Ref<string, Article> reference = new Ref<string, Article>();
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 1; n++)
                reference = article;
            var diff = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, diff);
            IsTrue(article == reference.Entity);

            // key (int) is value type
            var intEntity = new IntEntity { id = 1 };
            Ref <int, IntEntity> intRef = intEntity; // for one time allocations
            start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 1; n++)
                intRef = intEntity;
            diff = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreEqual(0, diff);
            IsTrue(intEntity == intRef.Entity);
        }
#endif
    }
}