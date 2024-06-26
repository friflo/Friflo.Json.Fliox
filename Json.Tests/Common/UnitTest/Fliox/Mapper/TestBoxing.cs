﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
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

            var start   = Mem.GetAllocatedBytes();
            LookupDictionaryJsonKey(dict, 100);
            var diff    = Mem.GetAllocationDiff(start);

            Console.Out.WriteLine($"diff: {diff}");
            
            Mem.NoAlloc(diff); // fails if running without debugger
        }
        
        private static void LookupDictionaryJsonKey(Dictionary<JsonKey, int> dict, int count) {
            for (int n = 0; n < count; n++) {
                var key = new JsonKey(n);
                dict.TryGetValue(key, out var value);
            }
        }
        
        [Test]
        public static void TestNoBoxingInt() {
            TestNoBoxing(1, 1);
        }
        
        [Test]
        public static void TestNoBoxingString() {
            TestNoBoxing("abc", "abc");
        }
        
        [Test]
        public static void TestNoBoxingGuid() {
            var guid = new Guid("11111111-0000-1111-0000-111111111111");
            TestNoBoxing(guid, guid);
        }

        private static void TestNoBoxing<TKey>(TKey left, TKey right) {
            IsTrue(EqualityComparer<TKey>.Default.Equals(left, right)); // force caching of default TKey 
            
            var start   = Mem.GetAllocatedBytes();
            var equal   = false;   
            for (int n = 0; n < 100; n++) {
                equal = EqualityComparer<TKey>.Default.Equals(left, right);
            }
            var diff    = Mem.GetAllocationDiff(start);
            
            Mem.NoAlloc(diff);
            IsTrue(equal);
        }
    }
}

