using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Json
{
    public static class TestJson2MsgPack
    {
        // --- null
        [Test]
        public static void Test_Msg2Json_null()
        {
            var json        = new JsonValue("true");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("C3", msg.DataHex());
            AssertEof(json, json2Msg);
        }
        
        // --- bool
        [Test]
        public static void Test_Msg2Json_true()
        {
            var json        = new JsonValue("null");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("C0", msg.DataHex());
            AssertEof(json, json2Msg);
        }
        
        // --- number
        [Test]
        public static void Test_Msg2Json_integer()
        {
            var json        = new JsonValue("255");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("CC FF", msg.DataHex());
            AreEqual((byte)MsgFormat.uint8, msg[0]);
        }
        
        [Test]
        public static void Test_Msg2Json_float()
        {
            var json        = new JsonValue("12.5");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("CA 41 48 00 00", msg.DataHex());
            AreEqual((byte)MsgFormat.float32, msg[0]);
        }
        
        [Test]
        public static void Test_Msg2Json_double()
        {
            var json        = new JsonValue("1222333444.5");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("CB 41 D2 36 D5 01 20 00 00", msg.DataHex());
            AreEqual((byte)MsgFormat.float64, msg[0]);
        }
        
        [Test]
        public static void Test_Msg2Json_string()
        {
            var json        = new JsonValue("\"abc\"");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("A3 61 62 63", msg.DataHex());
            AreEqual((byte)MsgFormat.fixstr, msg[0] & 0xe0);
        }
        
        // --- array
        [Test]
        public static void Test_Msg2Json_array_number()
        {
            var json        = new JsonValue("[1]");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("91 01", msg.DataHex());
            AreEqual((byte)MsgFormat.fixarray + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_array16()
        {
            var json        = new JsonValue("[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16]");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("DC 00 10 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10", msg.DataHex());
            AreEqual((byte)MsgFormat.array16, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_array32()
        {
            var chars    = new char[2 * 0x10000];
            for (int n = 0; n < 0x10000; n++) {
                chars[2 * n]        = (char)(n % 10 + 48); // 0-9
                chars[2 * n + 1]    = ',';
            }
            var jsonStr     = "[" + chars.AsSpan().ToString() + "127]";
            var json        = new JsonValue(jsonStr);
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual((byte)MsgFormat.array32, msg[0]);
            var hex         = msg.DataHex();
            That(hex, Does.StartWith("DD 00 01 00 01 00 01 02 03 04 05 06 07 08 09"));
            That(hex, Does.EndWith("00 01 02 03 04 05 06 07 08 09 00 01 02 03 04 05 7F"));
        }
        
        // --- object
        [Test]
        public static void Test_Msg2Json_object_null()
        {
            var json        = new JsonValue("{\"a\":null}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 C0", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_true()
        {
            var json        = new JsonValue("{\"a\":true}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 C3", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_int()
        {
            var json        = new JsonValue("{\"a\":1}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 01", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_float()
        {
            var json        = new JsonValue("{\"a\":1.5}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 CA 3F C0 00 00", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_string()
        {
            var json        = new JsonValue("{\"a\":\"xyz\"}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 A3 78 79 7A", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_array()
        {
            var json        = new JsonValue("{\"a\":[]}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 90", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap, msg[0] & 0xf0);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_object()
        {
            var json        = new JsonValue("{\"a\":{}}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("81 A1 61 80", msg.DataHex());
            AreEqual((byte)MsgFormat.fixmap + 1, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        [Test]
        public static void Test_Msg2Json_object_16()
        {
            var json        = new JsonValue("{\"a\":1,\"b\":2,\"c\":3,\"d\":4,\"e\":5,\"f\":6,\"g\":7,\"h\":8,\"i\":9,\"j\":10,\"k\":11,\"l\":12,\"m\":13,\"n\":14,\"o\":15,\"p\":16\n}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("DE 00 10 A1 61 01 A1 62 02 A1 63 03 A1 64 04 A1 65 05 A1 66 06 A1 67 07 A1 68 08 A1 69 09 A1 6A 0A A1 6B 0B A1 6C 0C A1 6D 0D A1 6E 0E A1 6F 0F A1 70 10", msg.DataHex());
            AreEqual((byte)MsgFormat.map16, msg[0]);
            AssertEof(json, json2Msg);
        }
        
        
        // -------------------------------------- utils --------------------------------------
        private static void AssertEof(JsonValue json, Json2MsgPack json2Msg)
        {
            int count = json.Count;
            for (int n = 0; n < count - 1; n++) {
                var subJson = new JsonValue(json.MutableArray, 0, n);
                var msg     = json2Msg.ToMsgPack(subJson);
                
                AreEqual(0, msg.Length);
                IsTrue(json2Msg.HasError);
            }
            {
                var array = new byte[count + 2];
                json.CopyTo(ref array);
                array[count]        = (byte)' '; // add space
                array[count + 1]    = (byte)'1'; // add an invalid character
                var invalidJson = new JsonValue(array);
                var msg     = json2Msg.ToMsgPack(invalidJson);
                
                AreEqual(0, msg.Length);
                IsTrue(json2Msg.HasError);
            }
        }
    }
}