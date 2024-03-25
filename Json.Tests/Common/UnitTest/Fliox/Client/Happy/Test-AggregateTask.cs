// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestHappy
    {
        [Test] public async Task TestAggregate      () { await TestCreate(async (store) => await AssertAggregate (store)); }
        
        private static async Task AssertAggregate(PocStore store) {
            var orders      = store.orders;
            var articles    = store.articles;

            var orderCount      = orders.CountAll()                             .TaskName("orderCount");
            var articleCount    = articles.Count(a => a.name == "Smartphone")   .TaskName("articleCount");

            await store.SyncTasks(); // ----------------
            
            AreEqual(2,     orderCount.Result);
            AreEqual(1,     articleCount.Result);
        }
    }
}