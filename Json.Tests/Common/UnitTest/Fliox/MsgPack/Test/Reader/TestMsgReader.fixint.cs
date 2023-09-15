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
                AreEqual("MessagePack error - expect uint8. was: -fixint(0xFC) pos: 0 (root)", reader.Error);
            }
        }
    }
}