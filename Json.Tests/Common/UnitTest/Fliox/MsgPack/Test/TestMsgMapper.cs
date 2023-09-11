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
    }
}