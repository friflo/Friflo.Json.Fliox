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
        public static void Read_uint8_FF()
        {
            var data = HexToSpan("cc ff"); // 255 (uint8)
            AreEqual((byte)MsgFormat.uint8, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt16();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadByte();
                AreEqual(255, x);
                AreEqual(data.Length, reader.Pos);
            }
        }
        
        [Test]
        public static void Read_int8_7F()
        {
            var data = HexToSpan("d0 7f"); // 127 (int8)
            AreEqual((byte)MsgFormat.int8, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt16();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadByte();
                AreEqual(127, x);
                AreEqual(data.Length, reader.Pos);
            }
        }
    }
}