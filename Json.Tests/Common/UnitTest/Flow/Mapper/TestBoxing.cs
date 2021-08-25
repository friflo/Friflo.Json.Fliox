// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Mapper
{
    public static class TestBoxing
    {
        [Test]
        public static void TestBoxingDictionaryJsonKey() {
            var dict = new Dictionary<JsonKey, int>(JsonKey.Equality);
            for (int n = 0; n < 100; n++)
                dict.Add(new JsonKey(n), n);    

            GC.Collect();
            LookupDictionaryJsonKey(dict, 2); // ensure subsequent calls dont allocate memory on heap

            var start   = GC.GetAllocatedBytesForCurrentThread();
            LookupDictionaryJsonKey(dict, 100);
            var diff    = GC.GetAllocatedBytesForCurrentThread() - start;

            Console.Out.WriteLine($"diff: {diff}");
            
            AreEqual(0, diff); // fails if running without debugger
        }
        
        private static void LookupDictionaryJsonKey(Dictionary<JsonKey, int> dict, int count) {
            for (int n = 0; n < count; n++) {
                var key = new JsonKey(n);
                dict.TryGetValue(key, out var value);
            }
        }
        
        [Test]
        public static void TestNoBoxing() {
            var generic = new Generic<int>();
            
            var start   = GC.GetAllocatedBytesForCurrentThread();
            var equal   = false;   
            for (int n = 0; n < 1000000; n++)
                equal = generic.IsEqual(0);
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