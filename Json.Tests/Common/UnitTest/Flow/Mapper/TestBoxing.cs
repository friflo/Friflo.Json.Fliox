// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Mapper
{
    public class TestBoxing
    {
        [Test]
        public void TestBoxingDictionary() {
            var dict = new Dictionary<JsonKey, int>(JsonKey.Equality);
            var key = new JsonKey(1);
            
            dict.Add(key, 2);
            var start   = GC.GetAllocatedBytesForCurrentThread();
            var value   = dict[key];
            var end     = GC.GetAllocatedBytesForCurrentThread();
            var diff    = end - start;
            
            // AreEqual(0, diff);
            AreEqual(2, value);
        }
    }
}