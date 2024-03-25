// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Host;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestEntitySetUtils
    {
        [Test]
        public static void TestEntitySetUtils_GetEntityKey()
        {
            var client = new EntityIdStore(new FlioxHub(new MemoryDatabase("test")));
            {
                var entity  = new GuidEntity { id = Guid.NewGuid() };
                var key     = client.guidEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.id, key);
            }
            {
                var entity  = new IntEntity { id = 42 };
                var key     = client.intEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.id, key);
            }
            {
                var entity  = new LongEntity { Id = 42 };
                var key     = client.longEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.Id, key);
            }
            {
                var entity  = new ShortEntity { id = 42 };
                var key     = client.shortEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.id, key);
            }
            {
                var entity  = new ByteEntity { id = 42 };
                var key     = client.byteEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.id, key);
            }
            {
                var entity  = new CustomIdEntity { customId = "c-1" };
                var key     = client.customIdEntities.Utils.GetEntityKey(entity);
                AreEqual(entity.customId, key);
            }
        }

        private const long Count = 1; // 1_000_000_000;

        [Test]
        public static void TestEntitySetUtils_GetEntityKey_Perf()
        {
            var client          = new EntityIdStore(new FlioxHub(new MemoryDatabase("test")));
            var articleUtils    = client.intEntities.Utils;
            
            var article         = new IntEntity { id = 42 };
            
            for (long n = 0; n < Count; n++) {
                _ = articleUtils.GetEntityKey(article);
                // _ = article.id;
            }
        }
    }
}