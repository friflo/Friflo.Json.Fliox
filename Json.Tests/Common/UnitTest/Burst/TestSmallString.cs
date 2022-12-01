// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Burst.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public static class TestSmallString
    {
        [Test]
        public static void SmallStringIsEqual() {
            var sb = new StringBuilder();
            for (int n = 0; n < 20; n++) {
                sb.Clear();
                for (int i = 0; i < n; i++) {
                    char c = (char)('a' + i);
                    sb.Append(c);
                }
                var str     = sb.ToString();
                var small  = new SmallString(str);
                
                IsTrue(small.IsEqual(small));
            }
        }
        
        [Test]
        public static void SmallStringNotEqual() {
            var sb = new StringBuilder();
            for (int n = 0; n < 20; n++) {
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
    }
}