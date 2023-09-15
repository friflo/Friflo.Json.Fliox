using System;
using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Fliox.MsgPack.Json;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static class TestMsgPack2Json
    {
        // --- null
        [Test]
        public static void Test_Json2Msg_null()
        {
            var data = HexToSpan("c0");
            AreEqual((byte)MsgFormat.nil, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("null", json.ToString());
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":null}", json.ToString());
        }
        
        // --- bool
        [Test]
        public static void Test_Json2Msg_false()
        {
            var data = HexToSpan("c2");
            AreEqual((byte)MsgFormat.False, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("false", json.ToString());
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":false}", json.ToString());
        }
        
        [Test]
        public static void Test_Json2Msg_true()
        {
            var data = HexToSpan("c3");
            AreEqual((byte)MsgFormat.True, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("true", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":true}", json.ToString());
        }
        
        // --- string
        [Test]
        public static void Test_Json2Msg_fixstr()
        {
            var data = HexToSpan("A3 61 62 63"); // "abc" (fixstr)
            AreEqual((byte)MsgFormat.fixstr, data[0] & 0xe0);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("\"abc\"", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":\"abc\"}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_str8()
        {
            var data = HexToSpan("D9 0A 30 31 32 33 34 35 36 37 38 39");
            AreEqual((byte)MsgFormat.str8, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("\"0123456789\"", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":\"0123456789\"}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_str16()
        {
            var data = HexToSpan("DA 00 0A 30 31 32 33 34 35 36 37 38 39");
            AreEqual((byte)MsgFormat.str16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("\"0123456789\"", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":\"0123456789\"}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_str32()
        {
            var data = HexToSpan("DB 00 00 00 0A 30 31 32 33 34 35 36 37 38 39");
            AreEqual((byte)MsgFormat.str32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("\"0123456789\"", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":\"0123456789\"}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // --- integer
        [Test]
        public static void Test_Json2Msg_fixint()
        {
            var data = HexToSpan("00");
            AreEqual((byte)MsgFormat.fixintPos, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("0", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":0}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_fixint_neg()
        {
            var data = HexToSpan("ff");
            AreEqual((byte)MsgFormat.fixintNegMax, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("-1", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":-1}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_uint8()
        {
            var data = HexToSpan("cc ff");
            AreEqual((byte)MsgFormat.uint8, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("255", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":255}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_int8()
        {
            var data = HexToSpan("d0 7f");
            AreEqual((byte)MsgFormat.int8, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("127", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":127}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_uint16()
        {
            var data = HexToSpan("cd ff ff"); // 65535 (uint16)
            AreEqual((byte)MsgFormat.uint16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("65535", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":65535}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_int16()
        {
            var data = HexToSpan("d1 7f ff"); // 32767 (int16)
            AreEqual((byte)MsgFormat.int16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("32767", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":32767}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_uint32()
        {
            var data = HexToSpan("ce ff ff ff ff"); // 4294967295 (uint32)
            AreEqual((byte)MsgFormat.uint32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("4294967295", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":4294967295}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_int32()
        {
            var data = HexToSpan("d2 7f ff ff ff"); // 2147483647 (int32)
            AreEqual((byte)MsgFormat.int32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("2147483647", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":2147483647}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_uint64()
        {
            var data = HexToSpan("cf 00 00 00 00 ff ff ff ff"); // 4294967295 (uint64)
            AreEqual((byte)MsgFormat.uint64, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("4294967295", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":4294967295}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_int64()
        {
            var data = HexToSpan("d3 00 00 00 00 ff ff ff ff"); // 4294967295 (int64)
            AreEqual((byte)MsgFormat.int64, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("4294967295", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":4294967295}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // --- float
        [Test]
        public static void Test_Json2Msg_float32()
        {
            var data = HexToSpan("ca 4A FF FF FE"); // 8388607 (float32)
            AreEqual((byte)MsgFormat.float32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("8388607", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":8388607}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_float64()
        {
            var data = HexToSpan("cb 41 EF FF FF FF E0 00 00"); // 4294967295 (float64)
            AreEqual((byte)MsgFormat.float64, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("4294967295", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":4294967295}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // --- bin (byte[])
        [Test]
        public static void Test_Json2Msg_bin8()
        {
            var data = HexToSpan("c4 01 09");           // [9]
            AreEqual((byte)MsgFormat.bin8, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[9]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[9]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_bin16()
        {
            var data = HexToSpan("c5 00 01 09");        // [9]
            AreEqual((byte)MsgFormat.bin16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[9]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[9]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_bin32()
        {
            var data = HexToSpan("c6 00 00 00 01 09");  // [9]
            AreEqual((byte)MsgFormat.bin32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[9]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[9]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // --- array
        [Test]
        public static void Test_Json2Msg_fixarray()
        {
            var data = HexToSpan("91 00");          // [0]
            AreEqual((byte)MsgFormat.fixarray, data[0] & 0xf0);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[0]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[0]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_array16()
        {
            var data = HexToSpan("dc 00 01 00");    // [0]
            AreEqual((byte)MsgFormat.array16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[0]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[0]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_array32()
        {
            var data = HexToSpan("dd 00 00 00 01 00"); // [0]
            AreEqual((byte)MsgFormat.array32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("[0]", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":[0]}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // --- map
        [Test]
        public static void Test_Json2Msg_fixmap()
        {
            var data = HexToSpan("81 A1 61 01"); // {"a":1}
            AreEqual((byte)MsgFormat.fixmap, data[0] & 0xf0);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("{\"a\":1}", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":{\"a\":1}}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_map16()
        {
            var data = HexToSpan("de 00 01 A1 61 01"); // {"a":1}
            AreEqual((byte)MsgFormat.map16, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("{\"a\":1}", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":{\"a\":1}}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        [Test]
        public static void Test_Json2Msg_map32()
        {
            var data = HexToSpan("df 00 00 00 01 A1 61 01"); // {"a":1}
            AreEqual((byte)MsgFormat.map32, data[0]);
            var msg2JSON = new MsgPack2Json();
            var json = msg2JSON.ToJson(data);
            AreEqual("{\"a\":1}", json.ToString());
            AssertEof(data, msg2JSON);
            //
            var obj = CreateObjectMsg(data);
            json = msg2JSON.ToJson(obj);
            AreEqual("{\"x\":{\"a\":1}}", json.ToString());
            AssertEof(obj, msg2JSON);
        }
        
        // -------------------------------------- utils --------------------------------------
        private static void AssertEof(ReadOnlySpan<byte> data, MsgPack2Json msg2JSON)
        {
            for (int n = 0; n < data.Length - 1; n++) {
                var subData = data.Slice(0, n);
                msg2JSON.ToJson(subData);
                AreEqual(MsgReaderState.UnexpectedEof, msg2JSON.ReaderState);
            }
        }
        
        private static ReadOnlySpan<byte> CreateObjectMsg(ReadOnlySpan<byte> data)
        {
            var msg = new byte[3 + data.Length];
            msg[0] = 0x81;  // fixmap - length: 1
            msg[1] = 0xA1;  // fixstr - length: 1
            msg[2] = 0x78;  // 'x'
            for (int n = 0; n < data.Length; n++) {
                msg[n + 3] = data[n];
            }
            return msg;
        }
    }
}