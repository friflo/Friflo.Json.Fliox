// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.LabString
{
    public static class TestStringIntern
    {
        private const long Count = 1000; // 1_000_000_000;
            
        [Test]
        public static void TestStringInternGet() {
            var hello1 = new string("hello");
            var hello2 = new string("hello");
            hello2.GetHashCode();
            
            var intern = new StringIntern();
            intern.Get2(hello1);
            
            for (int n = 0; n < Count; n++) {
                // hello2.GetHashCode();
                intern.Get(hello2);
            }
        }
        
        [Test]
        public static void TestStringInternGetHashCode() {
            for (int n = 0; n < Count; n++) {
                "hello".GetHashCode();
            }
        }
        
        [Test]
        public static void TestStringInternGetHashCode2() {
            for (int n = 0; n < Count; n++) {
                CalculateHash("hello");
            }
        }

        private static ulong CalculateHash(string read)
        {
            ulong hashedValue = 3074457345618258791ul;
            for(int i=0; i<read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }
        
        [Test]
        public static void TestStringInternPerfEquals() {
            var hello1 = new string("hello1");
            var hello2 = new string("hello2");
            TestStringInternPerfEqualsIntern(hello1, hello2);
        }
        
        private static void TestStringInternPerfEqualsIntern(string str1, string str2) {
            var result = false;
            for (long n = 0; n < Count; n++) {
                result = result || str1 != str2;
            }
            IsTrue(result);
        }
        
        [Test]
        public static void TestStringInternPerfReferenceEquals() {
            var result = false;
            var hello1 = new string("hello");
            
            for (long n = 0; n < Count; n++) {
                result = result || ReferenceEquals(hello1, hello1);
            }
            IsTrue(result);
        }
    }
}