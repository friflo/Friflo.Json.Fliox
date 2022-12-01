// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Common.UnitTest.Misc.LabString;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedVariable
// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public static class TestSmallStringPerf
    {
        private const           long    Count = 1000_000_000;
        public  static readonly string  Str1 = new string("----1111");
        public  static readonly string  Str2 = new string("----1111");
        
        [Test]
        public static void SmallString_Init() {
            for (long n = 0; n < Count; n++) {
                var small = new SmallString(Str1);
            }
        }
        
        [Test]
        public static void SmallString_Compare() {
            var small1 = new SmallString(Str1);
            var small2 = new SmallString(Str2);

            bool result = false;
            for (long n = 0; n < Count; n++) {
                result = small1.IsEqual(small2);
            }
            Console.WriteLine(result);
        }

        // ------------- various benchmarks used by alternative methods to compare strings -------------
        [Test]
        public static void SmallString_GetHashCode() {
            for (int n = 0; n < Count; n++) {
                var hash = Str1.GetHashCode();
            }
        }
        
        [Test]
        public static void SmallString_Equals() {
            SmallString_EqualsIntern(Str1, Str2);
        }
        
        private static void SmallString_EqualsIntern(string str1, string str2) {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = str1 == str2;
            }
            Console.WriteLine(result);
        }
        
        [Test]
        public static void SmallString_ReferenceEquals() {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = result || ReferenceEquals(Str1, Str2);
            }
            Console.WriteLine(result);
        }

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