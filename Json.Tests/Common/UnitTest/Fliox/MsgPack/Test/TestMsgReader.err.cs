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
        private delegate void ReadAction (ref MsgReader context);
        
        /* [Test]
        public static void Read_Eof()
        {
            var data = HexToSpan("");
            AssertEof(data, (ref MsgReader r) => r.ReadFloat64(),   "MessagePack error - unexpected EOF. pos: 0 (root)");
            AssertEof(data, (ref MsgReader r) => r.ReadFloat32(),   "MessagePack error - unexpected EOF. pos: 0 (root)");
            AssertEof(data, (ref MsgReader r) => r.ReadInt64(),     "MessagePack error - unexpected EOF. pos: 0 (root)");
            AssertEof(data, (ref MsgReader r) => r.ReadInt32(),     "MessagePack error - unexpected EOF. pos: 0 (root)");
            AssertEof(data, (ref MsgReader r) => r.ReadInt16(),     "MessagePack error - unexpected EOF. pos: 0 (root)");
            AssertEof(data, (ref MsgReader r) => r.ReadByte(),      "MessagePack error - unexpected EOF. pos: 0 (root)");
        } */
        
        private static void AssertEof(ReadOnlySpan<byte> data, ReadAction action, string error)
        {
            // --- OK
            var ok = new MsgReader(data);
            action(ref ok);
            IsNull (ok.Error);
            AreNotEqual(MsgReader.MsgError, ok.Pos);
            
            // --- Empty
            var empty = new MsgReader(data.Slice(0,0));
            action(ref empty);
            AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", empty.Error);
            AreEqual(MsgReader.MsgError, empty.Pos);
            
            for (int n = 1; n < data.Length; n++)
            {
                // --- read data sub-section
                var subData = data.Slice(0, n);
                var reader = new MsgReader(subData);
                action(ref reader);
                AreEqual(error, reader.Error);
                AreEqual(MsgReader.MsgError, reader.Pos);
                
                // --- skip data sub-section
                var skipReader = new MsgReader(subData);
                skipReader.SkipTree();
                AreEqual(error, skipReader.Error);
                AreEqual(MsgReader.MsgError, skipReader.Pos);
            }
        }
        
        [Test]
        public static void Read_Eof_data()
        {
            // --- double
            {
                var data = HexToSpan("cb 00 00 00 00 00 00 00 00"); // 0 (double)
                AreEqual((byte)MsgFormat.float64, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadFloat64(), "MessagePack error - unexpected EOF. type: float64(0xCB) pos: 0 (root)");
            }
            // --- float
            {
                var data = HexToSpan("ca 00 00 00 00");             // 0 (float)
                AreEqual((byte)MsgFormat.float32, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadFloat32(), "MessagePack error - unexpected EOF. type: float32(0xCA) pos: 0 (root)");
            }
            // --- signed int
            {
                var data = HexToSpan("d3 00 00 00 00 ff ff ff ff"); // 4294967295 (uint64)
                AreEqual((byte)MsgFormat.int64, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt64(), "MessagePack error - unexpected EOF. type: int64(0xD3) pos: 0 (root)");
            } {
                var data = HexToSpan("d2 7f ff ff ff");             // 2147483647 (int32)
                AreEqual((byte)MsgFormat.int32, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt32(), "MessagePack error - unexpected EOF. type: int32(0xD2) pos: 0 (root)");
            } {
                var data = HexToSpan("d1 7f ff");                   // 32767 (int16)
                AreEqual((byte)MsgFormat.int16, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt16(), "MessagePack error - unexpected EOF. type: int16(0xD1) pos: 0 (root)");
            } {
                var data = HexToSpan("d0 7f");                      // 127 (int8)
                AreEqual((byte)MsgFormat.int8, data[0]);

                AssertEof(data, (ref MsgReader r) => r.ReadByte(), "MessagePack error - unexpected EOF. type: int8(0xD0) pos: 0 (root)");
            }
            // --- unsigend signed int
            {
                var data = HexToSpan("cf 00 00 00 00 ff ff ff ff");
                AreEqual((byte)MsgFormat.uint64, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt64(), "MessagePack error - unexpected EOF. type: uint64(0xCF) pos: 0 (root)");
            } {
                var data = HexToSpan("ce 7f ff ff ff");
                AreEqual((byte)MsgFormat.uint32, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt32(), "MessagePack error - unexpected EOF. type: uint32(0xCE) pos: 0 (root)");
            } {
                var data = HexToSpan("cd 7f ff");
                AreEqual((byte)MsgFormat.uint16, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadInt16(), "MessagePack error - unexpected EOF. type: uint16(0xCD) pos: 0 (root)");
            } {
                var data = HexToSpan("cc 7f");
                AreEqual((byte)MsgFormat.uint8, data[0]);
                AssertEof(data, (ref MsgReader r) => r.ReadByte(), "MessagePack error - unexpected EOF. type: uint8(0xCC) pos: 0 (root)");
            }
        }
        
        [Test]
        public static void Read_str()
        {
            var data = HexToSpan("A3 61 62 63"); // "abc" (fixstr)
            {
                var reader = new MsgReader(data);
                reader.ReadFloat64();
                AreEqual("MessagePack error - expect float64 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadFloat32();
                AreEqual("MessagePack error - expect float32 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt64();
                AreEqual("MessagePack error - expect int64 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt32();
                AreEqual("MessagePack error - expect int32 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadInt16();
                AreEqual("MessagePack error - expect int16 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            } {
                var reader = new MsgReader(data);
                reader.ReadByte();
                AreEqual("MessagePack error - expect uint8 compatible type. was: fixstr(0xA3) pos: 0 (root)", reader.Error);
            }
        }
    }
}