using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{
    public static partial class TestMsgReader
    {
        
        
       
        
        
        [Test]
        public static void Read_str()
        {
            var data = HexToSpan("A3 61 62 63"); // "abc" (fixstr)
            {
                var reader = new MsgReader(data);
                reader.ReadFloat64();
                AreEqual("MessagePack error - expect float64. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - expect float32. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - expect int64. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - expect int32. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - expect int16. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - expect uint8. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            }
        }
    }
}