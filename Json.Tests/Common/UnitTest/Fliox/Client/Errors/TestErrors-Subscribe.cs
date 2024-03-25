// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Errors
{
    public partial class TestErrors
    {
        [Test] public void TestSubscribePushEventsError() {
            var hub = new HttpClientHub("main_db", "http://localhost:8010/fliox/");
            var store = new PocStore(hub) { UserId = "user", Token = "token", ClientId = "sub-client"};
            
            var e = Throws<InvalidOperationException>(() => {
                store.articles.SubscribeChanges(Change.All, (changes, context) => {});
            });
            AreEqual("The FlioxHub used by the client don't support PushEvents. hub: HttpClientHub", e.Message);
        }
    }
}