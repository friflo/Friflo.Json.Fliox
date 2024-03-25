// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map.Val;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    // --- CLS compliant
    internal enum EnumInt    : int       { Value1 = Int32.MaxValue  }
    internal enum EnumByte   : byte      { Value1 = Byte.MaxValue   } 
    internal enum EnumShort  : short     { Value1 = Int16.MaxValue  }
    internal enum EnumLong   : long      { Value1 = Int64.MaxValue  }
    
    // --- non CLS compliant
    internal enum EnumUInt   : uint      { Value1 = UInt32.MaxValue }
    internal enum EnumSByte  : sbyte     { Value1 = SByte.MaxValue  }
    internal enum EnumUShort : ushort    { Value1 = UInt16.MaxValue }
    
    public static class TestEnumConvert
    {
        // --- CLS compliant
        [Test]
        public static void TestEnumConvertInt() {
            var convert = EnumConvert.GetEnumConvert<EnumInt>();
            var start   = Mem.GetAllocatedBytes();
            var e       = convert.LongToEnum(Int32.MaxValue);
            var diff    = Mem.GetAllocationDiff(start);
            AreEqual(EnumInt.Value1, e);
            Mem.NoAlloc(diff);
        }
        
        [Test]
        public static void TestEnumConvertLong() {
            var convert = EnumConvert.GetEnumConvert<EnumLong>();
            var e = convert.LongToEnum(Int64.MaxValue);
            AreEqual(EnumLong.Value1, e);
        }
        
        [Test]
        public static void TestEnumConvertShort() {
            var convert = EnumConvert.GetEnumConvert<EnumShort>();
            var e = convert.LongToEnum(Int16.MaxValue);
            AreEqual(EnumShort.Value1, e);
        }
        
        [Test]
        public static void TestEnumConvertByte() {
            var convert = EnumConvert.GetEnumConvert<EnumByte>();
            var e = convert.LongToEnum(Byte.MaxValue);
            AreEqual(EnumByte.Value1, e);
        }
        
        // --- non CLS compliant
        [Test]
        public static void TestEnumConvertUInt() {
            var convert = EnumConvert.GetEnumConvert<EnumUInt>();
            var e = convert.LongToEnum(UInt32.MaxValue);
            AreEqual(EnumUInt.Value1, e);
        }
        
        [Test]
        public static void TestEnumConvertUShort() {
            var convert = EnumConvert.GetEnumConvert<EnumUShort>();
            var e = convert.LongToEnum(UInt16.MaxValue);
            AreEqual(EnumUShort.Value1, e);
        }
        
        [Test]
        public static void TestEnumConvertSByte() {
            var convert = EnumConvert.GetEnumConvert<EnumSByte>();
            var e = convert.LongToEnum(SByte.MaxValue);
            AreEqual(EnumSByte.Value1, e);
        }
    }
}