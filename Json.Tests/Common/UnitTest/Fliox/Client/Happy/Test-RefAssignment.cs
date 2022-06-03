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