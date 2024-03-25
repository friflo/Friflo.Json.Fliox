// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Tests.Common.UnitTest.Misc;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public static class TestSmallString
    {
        [Test]
        public static void SmallStringIsEqual() {
            var smallNull = new SmallString();
            var sb = new StringBuilder();
            for (int n = 0; n < 30; n++) {
                sb.Clear();
                for (int i = 0; i < n; i++) {
                    char c = (char)('a' + i);
                    sb.Append(c);
                }
                var str     = sb.ToString();
                var small   = new SmallString(str);
                
                AreEqual(n, small.Length);
                IsFalse (small.IsNull());
                IsTrue  (small.IsEqual(small));
                
                IsFalse (smallNull.IsEqual(small));
                IsFalse (small.IsEqual(smallNull));
            }
        }
        
        [Test]
        public static void SmallStringNull() {
            var null1 = new SmallString();
            IsTrue(null1.IsNull());
            
            var null2 = new SmallString(null);
            IsTrue(null2.IsNull());
            
            IsNull(null1.ToString());
            
            Throws<NullReferenceException>(() => {
                var _ = null1.Length;
            });
        }
        
        [Test]
        public static void SmallStringNotEqual() {
            var sb = new StringBuilder();
            for (int n = 0; n < 30; n++) {
                sb.Clear();
                for (int i = 0; i < n; i++) {
                    char c = (char)('a' + i);
                    sb.Append(c);
                }
                var str     = sb.ToString(); 
                var str1    = str + "-";
                var str2    = str + "#";
                var small1  = new SmallString(str1);
                var small2  = new SmallString(str2);
                
                IsFalse(small1.IsEqual(small2));
            }
        }
        
        [Test]
        public static void SmallStringMaxCharIsEqual() {
            char charMax = char.MaxValue; // 0xFFFF;             
            for (int n = 0; n < 20; n++) {
                var str     = new string(charMax, n); 
                var small   = new SmallString(str);
                IsTrue(small.IsEqual(small));
            }
        }
        
        [Test]
        public static void SmallStringAsKey() {
            var map = new Dictionary<SmallString, string>(SmallString.Equality);
            
            var key1 = new SmallString("key1");
            var key2 = new SmallString("key2");
            
            map[key1] = "value1";
            map[key2] = "value2";
            var _   = map[key1];
            var __  = map[key1]; // force one time allocations
            
            long start = Mem.GetAllocatedBytes();
            
            var value1 = map[key1];
            var value2 = map[key2];

            AreEqual("value1", value1);
            AreEqual("value2", value2);
        }
        
        [Test]
        public static void SmallStringAssertions() {
            var str     = new SmallString("str");
            
            Throws<NotImplementedException>(() => {
                var _ = str.GetHashCode();
            });
            Throws<NotImplementedException>(() => {
                var _ = str.Equals(default);
            });
        }
    }
}