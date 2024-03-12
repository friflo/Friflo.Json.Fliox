// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    
    public class TestNonClsClass
    {
        public ulong[]      ulongArray;
        public List<ulong>  ulongList;
        //
        public ulong    lngFld;
        public uint     intFld;
        public ushort   srtFld;
        public sbyte    bytFld;
        //
        public ulong    lngPrp { get; set; }
        public uint     intPrp { get; set; }
        public ushort   srtPrp { get; set; }
        public sbyte    bytPrp { get; set; }
    }
    
    public struct TestNonClsStruct
    {
        public ulong[]      ulongArray;
        public List<ulong>  ulongList;
        //
        public ulong    lngFld;
        public uint     intFld;
        public ushort   srtFld;
        public sbyte    bytFld;
        //
        // public ulong    lngPrp { get; set; }
        // public uint     intPrp { get; set; }
        // public ushort   srtPrp { get; set; }
        // public sbyte    bytPrp { get; set; }
    }

    public static class TestReadWriteNonCls
    {

        [Test]
        public static void TestReadWriteNonCls_Mapper()
        {
            string testJson = $@"
{{
    ""ulongArray"": [0, 18446744073709551615],
    ""ulongList"":  [1, 18446744073709551615],

    ""lngPrp"": 100,
    ""intPrp"": 101,
    ""srtPrp"": 102,
    ""bytPrp"": 103,

    ""lngFld"": 202,
    ""intFld"": 203,
    ""srtFld"": 204,
    ""bytFld"": 25
}}";
           
            var typeStore    = new TypeStore();
            var reader       = new ObjectReader(typeStore);
            var writer       = new ObjectWriter(typeStore);
            // --- class
            {            
                var testClass = reader.Read<TestNonClsClass>(testJson);
                AssertClassMembers(testClass);
                
                var json = writer.Write(testClass);
                testClass = reader.Read<TestNonClsClass>(json);
                AssertClassMembers(testClass);
            }
            // --- struct
            {            
                var testStruct = reader.Read<TestNonClsStruct>(testJson);
                AssertStructMembers(testStruct);
                
                var json = writer.Write(testStruct);
                var obj = reader.Read<TestNonClsStruct>(json);
                AssertStructMembers(obj);
            }
        }
        
        private static void AssertClassMembers(TestNonClsClass obj)
        {
            AreEqual(new ulong[]    {0, 18446744073709551615}, obj.ulongArray);
            AreEqual(new List<ulong>{1, 18446744073709551615}, obj.ulongList);
            
            AreEqual(100, obj.lngPrp);
            AreEqual(101, obj.intPrp);
            AreEqual(102, obj.srtPrp);
            AreEqual(103, obj.bytPrp);
            
            AreEqual(202, obj.lngFld);
            AreEqual(203, obj.intFld);
            AreEqual(204, obj.srtFld);
            AreEqual( 25, obj.bytFld);   
        }
        
        private static void AssertStructMembers(TestNonClsStruct obj)
        {
            AreEqual(new ulong[]    {0, 18446744073709551615}, obj.ulongArray);
            AreEqual(new List<ulong>{1, 18446744073709551615}, obj.ulongList);
            
            AreEqual(202, obj.lngFld);
            AreEqual(203, obj.intFld);
            AreEqual(204, obj.srtFld);
            AreEqual( 25, obj.bytFld);   
        } 
    }
}