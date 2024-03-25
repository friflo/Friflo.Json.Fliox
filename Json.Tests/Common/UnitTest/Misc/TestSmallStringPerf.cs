// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Tests.Common.UnitTest.Misc;
using Friflo.Json.Tests.Common.UnitTest.Misc.LabString;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedVariable
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public static class TestSmallStringPerf
    {
        private const           long    Count = 40; // 40_000_000;
        public  static readonly string  Str1 = new Span<char>("---1".ToCharArray()).ToString();
        public  static readonly string  Str2 = new Span<char>("---2".ToCharArray()).ToString();
        
        [Test]
        public static void SmallString_Init() {
            for (long n = 0; n < Count; n++) {
                var small = new SmallString(Str1);
            }
        }

        /// <summary>
        /// Outperform string compare in Unity by factor 10x - 20x
        /// Outperform string compare in CLR by factor 5x
        /// <br/>
        /// Costs - Requires one time <see cref="SmallString"/> instantiation
        /// Unity: 0.45 of string equals
        /// CLR:   1.5  of string equals
        /// </summary>
        [Test]
        public static void SmallString_IsEqual() {
            var small1 = new SmallString(Str1);
            var small2 = new SmallString(Str2);

            bool result = false;
            for (long n = 0; n < Count; n++) {
                result = small1.IsEqual(small2);
            }
            Console.WriteLine(result);
        }

        // ------------- various benchmarks used by alternative methods to compare strings -------------
        /// <summary>
        /// Would be required by by a 'string interning' implementation.
        /// Unity   2.4 <see cref="SmallString.IsEqual"/>
        /// CLR     7.0 <see cref="SmallString.IsEqual"/>
        /// <br/>
        /// A 'string interning' implementation required also a Dictionary or an array storing the references
        /// which lookup costs are even higher. 
        /// </summary>
        [Test]
        public static void SmallString_GetHashCode() {
            for (int n = 0; n < Count; n++) {
                var hash = Str1.GetHashCode();
            }
        }
        
        [Test]
        public static void SmallString_StringEquals() {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = Str1 == Str2;
            }
            Console.WriteLine(result);
        }
        
        /// <summary>
        /// Benchmark comparison for <see cref="SmallString.IsEqual"/> and using <see cref="Object.ReferenceEquals"/>
        /// </summary>
        [Test]
        public static void SmallString_ReferenceEquals() {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = result || ReferenceEquals(Str1, Str2);
            }
            Console.WriteLine(result);
        }

        /// <summary>
        /// Benchmark comparison for <see cref="SmallString.IsEqual"/> and comparing a value type
        /// </summary>
        [Test]
        public static void SmallString_IntEquals() {
            PerfIntEquals(1,2);
        }
        
        private static void PerfIntEquals(int val1, int val2) {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = result || val1 == val2;
            }
            IsFalse(result);
        }
        
        [Test]
        public static void SmallString_Dictionary() {
            var key1 = new SmallString("0123456789abcdef");
            var map = new Dictionary<SmallString, int>(SmallString.Equality);
            map[key1] = 1;
            for (int n= 0; n < Count; n++) {
                var v = map[key1];
            }
            AreEqual(1, map.Count);
        }
        
        [Test]
        public static void SmallString_DictionaryReference() {
            var key1 = "0123456789abcdef";
            var map = new Dictionary<string, int>();
            map[key1] = 1;
            for (int n= 0; n < Count; n++) {
                var v = map[key1];
            }
            AreEqual(1, map.Count);
        }
        
        // [Test]
        public static void SmallString_InternGet() {
            var intern = new StringIntern();
            intern.Get(Str1);
            
            for (int n = 0; n < Count; n++) {
                intern.Get(Str2);
            }
        }
    }
}