using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgFormatUtils;


// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test
{

    public static class TestMsgReader
    {
        [Test]
        public static void Read_uint64_FFFF_FFFF()
        {
            var data = HexToSpan("cf 00 00 00 00 ff ff ff ff"); // 4294967295 (uint64)
            AreEqual((byte)MsgFormat.uint64, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(4294967295, x);
            }         {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(4294967295, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(4294967295, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 4294967295 uint64(0xCF) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_fixint()
        {
            var data = HexToSpan("04"); // 4 (+fixint)
            IsTrue(data[0] < (byte)MsgFormat.fixintPosMax);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(4.0f, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt16();
                AreEqual(4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadByte();
                AreEqual(4, x);
            }
        }
        
        [Test]
        public static void Read_fixint_negative()
        {
            var data = HexToSpan("FC"); // -4 (-fixint)
            IsTrue(data[0] > (byte)MsgFormat.fixintNeg);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(-4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(-4.0f, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(-4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(-4, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt16();
                AreEqual(-4, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - expect uint8 compatible type. was: -fixint(0xFC) pos: 0 (root)", reader.Error);
            }
        }
            
            
        [Test]
        public static void Read_Boolean()
        {
            {
                var data = HexToSpan("C2"); // false
                AreEqual((byte)MsgFormat.False, data[0]);
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsFalse(x);
            } {
                var data = HexToSpan("C3"); // true
                AreEqual((byte)MsgFormat.True, data[0]);
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsTrue(x);
            } {
                var data = HexToSpan("04"); // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadBool();
                AreEqual("MessagePack error - expect boolean. was: +fixint(0x4) pos: 0 (root)", reader.Error);
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
            }
            {
                var data = HexToSpan("D9 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str8, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                var data = HexToSpan("DA 00 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str16, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                var data = HexToSpan("DB 00 00 00 0A 30 31 32 33 34 35 36 37 38 39");
                AreEqual((byte)MsgFormat.str32, data[0]);
                
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                var data = HexToSpan("04"); // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadString();
                AreEqual("MessagePack error - expect string or null. was: +fixint(0x4) pos: 0 (root)", reader.Error);
            }
        }
        

    }
}