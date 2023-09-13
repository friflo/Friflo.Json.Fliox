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
        public static void Read_float32()
        {
            var data = HexToSpan("ca 4A FF FF FE"); // 8388607 (float32)
            var value = BitConverter.SingleToInt32Bits(8388607);
            
            AreEqual((byte)MsgFormat.float32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt64();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadInt32();
                AreEqual(8388607, x);
                AreEqual(data.Length, reader.Pos);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - value out of range. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - value out of range. was: 8388607 float32(0xCA) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_float32_max()
        {
            var data = HexToSpan("ca 7F 7F FF FF"); // float.MaxValue (float32)
            var value = BitConverter.SingleToInt32Bits(float.MaxValue);
            
            AreEqual((byte)MsgFormat.float32, data[0]);
            {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat64();
                AreEqual(float.MaxValue, x);
            } {
                var reader = new MsgReader(data);
                var x = reader.ReadFloat32();
                AreEqual(float.MaxValue, x);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual(ErrFloat32, reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual(ErrFloat32, reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual(ErrFloat32, reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual(ErrFloat32, reader.Error);
            }
        }
        
#if UNITY_5_3_OR_NEWER
        private const string ErrFloat64 = "MessagePack error - value out of range. was: 1.79769313486232E+308 float64(0xCB) pos: 0 (root)";
        private const string ErrFloat32 = "MessagePack error - value out of range. was: 3.40282346638529E+38 float32(0xCA) pos: 0 (root)";
#else
        private const string ErrFloat64 = "MessagePack error - value out of range. was: 1.7976931348623157E+308 float64(0xCB) pos: 0 (root)";
        private const string ErrFloat32 = "MessagePack error - value out of range. was: 3.4028235E+38 float32(0xCA) pos: 0 (root)";
#endif
       
    }
}