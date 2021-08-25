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
            GC.Collect();
            
            var start   = GC.GetAllocatedBytesForCurrentThread();
            var value   = dict[key];
            var diff    = GC.GetAllocatedBytesForCurrentThread() - start;
            
            // AreEqual(0, diff); // fails if running without debugger
            AreEqual(2, value);
        }
        
        [Test]
        public void TestNoBoxing() {
            var generic = new Generic<int>();
            
            var start   = GC.GetAllocatedBytesForCurrentThread();
            var equal   = generic.IsEqual(0);
            var diff    = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreEqual(0, diff);
            IsTrue(equal);
        }
        
        
        private class Generic<TKey>
        {
            public bool IsEqual(TKey key) {
                return EqualityComparer<TKey>.Default.Equals(key, default);
            }
        }

    }
}