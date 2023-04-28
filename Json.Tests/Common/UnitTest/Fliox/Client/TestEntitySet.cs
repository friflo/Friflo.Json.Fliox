// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestEntitySet
    {
        [Test]
        public static void TestEntitySetUtils() {
            var hub     = new FlioxHub(new MemoryDatabase("test"));
            var client  = new PocStore(hub);
            
            var utils   = client.articles.Utils;
            var article = new Article { id = "abc" };
            
            AreEqual("abc", utils.GetEntityKey(article));
            AreEqual("abc", utils.GetEntityId(article).AsString());
            
            utils.SetEntityId(article, new JsonKey("xyz"));
            AreEqual("xyz", article.id);
            
            utils.SetEntityKey(article, "123");
            AreEqual("123", article.id);
            
            AreEqual("abc", utils.IdToKey(new JsonKey("abc")));
            AreEqual("xyz", utils.KeyToId("xyz").AsString());
            
            unsafe {
                var size = sizeof(SetUtils<string,Article>);
                AreEqual(1, size);
            }
        }
    }
}