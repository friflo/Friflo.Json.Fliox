using Friflo.Json.Fliox;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
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
        }
        
        // --- bool
        [Test]
        public static void Test_Msg2Json_true()
        {
            var json        = new JsonValue("null");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("C0", msg.DataHex());
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
            AreEqual("CB 40 29 00 00 00 00 00 00", msg.DataHex());
            AreEqual((byte)MsgFormat.float64, msg[0]);              // TODO should create float
        }
        
        // --- object
        [Test]
        public static void Test_Msg2Json_object()
        {
            var json        = new JsonValue("{\"a\":1}");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("DF 00 00 00 01 A1 61 01", msg.DataHex());
            AreEqual((byte)MsgFormat.map32, msg[0]);
        }
        
        // --- array
        [Test]
        public static void Test_Msg2Json_array()
        {
            var json        = new JsonValue("[1]");
            var json2Msg    = new Json2MsgPack();
            var msg         = json2Msg.ToMsgPack(json);
            AreEqual("DD 00 00 00 01 01", msg.DataHex());
            AreEqual((byte)MsgFormat.array32, msg[0]);
        }
    }
}