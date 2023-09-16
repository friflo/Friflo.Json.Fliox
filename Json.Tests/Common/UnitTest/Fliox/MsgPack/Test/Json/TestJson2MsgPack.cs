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