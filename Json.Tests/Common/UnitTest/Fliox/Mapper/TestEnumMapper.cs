// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map.Val;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    enum EnumInt            { Value1 = 1 }
    enum EnumByte : byte    { Value2 = 2 }
    
    public static class TestEnumMapper
    {
        [Test]
        public static void TestEnumDictionaryBoxing() {
            var dict = new Dictionary<EnumByte, int> { { EnumByte.Value2 , 1}};
            var e = EnumByte.Value2;
            dict.TryGetValue(e, out _);
            var start   = GC.GetAllocatedBytesForCurrentThread();
            dict.TryGetValue(e, out _);
            var dif     = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
        }
        
        [Test]
        public static void TestEnumConvertInt() {
            {
                var convert = EnumConvert.GetEnumConvert<EnumInt>();
                var start   = GC.GetAllocatedBytesForCurrentThread();
                var e       = convert.IntToEnum(1);
                var dif     = GC.GetAllocatedBytesForCurrentThread() - start;
                AreEqual(EnumInt.Value1, e);
                AreEqual(0, dif);
            }
        }
        
        [Test]
        public static void TestEnumConvertByte() {
            {
                var convert = EnumConvert.GetEnumConvert<EnumByte>();
                var e = convert.IntToEnum(2);
                AreEqual(EnumByte.Value2, e);
            }
        }
    }
}