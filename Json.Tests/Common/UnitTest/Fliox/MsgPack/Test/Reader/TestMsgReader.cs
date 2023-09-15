using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;


// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Reader
{

    public static partial class TestMsgReader
    {
        [Test]
        public static void Read_Boolean()
        {
            {
                var data = HexToSpan("C2"); // false
                AreEqual((byte)MsgFormat.False, data[0]);
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsFalse(x);
                AssertSkip(data);
            } {
                var data = HexToSpan("C3"); // true
                AreEqual((byte)MsgFormat.True, data[0]);
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsTrue(x);
                AssertSkip(data);
            } {
                var data = HexToSpan("04"); // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadBool();
                AreEqual("MessagePack error - expect bool. was: +fixint(0x4) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_String()
        {
            {
                var data = HexToSpan("A3 61 62 63"); // "abc" (fixstr)
                AreEqual((byte)MsgFormat.fixstr, data[0] & 0xe0);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("abc", x);
                AssertSkip(data);
            }
            {
                var data = HexToSpan("D9 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str8, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
                AssertSkip(data);
            }
            {
                var data = HexToSpan("DA 00 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str16, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
                AssertSkip(data);
            }
            {
                var data = HexToSpan("DB 00 00 00 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str32, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
                AssertSkip(data);
            }
            {
                var data = HexToSpan("04"); // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadString();
                AreEqual("MessagePack error - expect string or null. was: +fixint(0x4) pos: 0 (root)", reader.Error);
            }
        }
        

        internal static void AssertSkip(ReadOnlySpan<byte> data) {
            var reader = new MsgReader(data);
            reader.SkipTree();
            IsNull(reader.Error);
            AreEqual(data.Length, reader.Pos);
        }
        

    }
}