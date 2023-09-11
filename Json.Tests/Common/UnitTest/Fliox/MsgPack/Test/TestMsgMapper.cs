using System.Collections.Generic;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static class TestMsgMapper
    {
        [Test]
        public static void Write_Int32()
        {
            var mapper  = new MsgPackMapper();
            var value   = mapper.Write(42);
            
            AreEqual("2A", mapper.DataHex);
            
            var result = MsgPackMapper.Deserialize<int>(value);
            AreEqual(42, result);
        }
        
        [Test]
        public static void Write_List_Int32()
        {
            var mapper  = new MsgPackMapper();
            var list    = new List<int> { 42 }; 
            var data    = mapper.Write(list);
            
            AreEqual("91 2A", mapper.DataHex);
            
            var result = MsgPackMapper.Deserialize<List<int>>(data);
            AreEqual(1,  result.Count);
            AreEqual(42, result[0]);
        }
        
        [Test]
        public static void Write_Array_Int32()
        {
            var mapper  = new MsgPackMapper();
            var array   = new int[] { 42 }; 
            var data    = mapper.Write(array);
            
            AreEqual("91 2A", mapper.DataHex);
            
            var result = MsgPackMapper.Deserialize<int[]>(data);
            AreEqual(1,  result.Length);
            AreEqual(42, result[0]);
        }
        
        [Test]
        public static void Write_List_Class()
        {
            var mapper  = new MsgPackMapper();
            var list    = new List<Sample> { new Sample { x = 42 } }; 
            var data    = mapper.Write(list);
            
            AreEqual("91 82 A1 78 2A A5 63 68 69 6C 64 C0", mapper.DataHex);
            
            var result = MsgPackMapper.Deserialize<List<Sample>>(data);
            AreEqual(1,  result.Count);
            AreEqual(42, result[0].x);
        }
        
        [Test]
        public static void Write_Array_Class()
        {
            var mapper  = new MsgPackMapper();
            var array   = new Sample[] { new Sample { x = 42 } }; 
            var data    = mapper.Write(array);
            
            AreEqual("91 82 A1 78 2A A5 63 68 69 6C 64 C0", mapper.DataHex);
            
            var result = MsgPackMapper.Deserialize<Sample[]>(data);
            AreEqual(1,  result.Length);
            AreEqual(42, result[0].x);
        }
    }
}