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
    delegate void ReadAction (ref MsgReader context);
    
    public static class TestMsgReaderEof
    {
        private static void AssertEof(ReadOnlySpan<byte> data, ReadAction action, string error)
        {
            // --- read
            {
                var reader = new MsgReader(data);
                action(ref reader);
                AreEqual(MsgReaderState.Ok, reader.State);
                AreEqual(data.Length, reader.Pos);
            }
            // --- skip
            {
                var reader = new MsgReader(data);
                reader.SkipTree();
                AreEqual(MsgReaderState.Ok, reader.State);
                AreEqual(data.Length, reader.Pos);
            }
            var empty = data.Slice(0,0);
            // --- read empty
            {
                var reader = new MsgReader(empty);
                action(ref reader);
                AreEqual(MsgReaderState.UnexpectedEof, reader.State);
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            }
            // --- skip empty
            {
                var reader = new MsgReader(empty);
                reader.SkipTree();
                AreEqual(MsgReaderState.UnexpectedEof, reader.State);
                AreEqual("MessagePack error - unexpected EOF. pos: 0 (root)", reader.Error);
            }
            
            for (int n = 1; n < data.Length; n++)
            {
                // --- read data sub-section
                var subData = data.Slice(0, n);
                var reader = new MsgReader(subData);
                action(ref reader);
                AreEqual(MsgReaderState.UnexpectedEof, reader.State);
                if (error != null) { 
                    AreEqual(error, reader.Error);
                }
                
                // --- skip data sub-section
                var skipReader = new MsgReader(subData);
                skipReader.SkipTree();
                AreEqual(MsgReaderState.UnexpectedEof, skipReader.State);
                if (error != null) {
                    AreEqual(error, skipReader.Error);
                }
            }
        }
        
        // --- nil
        [Test]
        public static void Read_Eof_nil()
        {
            var data = HexToSpan("c0"); // 0 (double)
            AreEqual((byte)MsgFormat.nil, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadString(), null);
        }
        
        // --- bool
        [Test]
        public static void Read_Eof_bool()
        {
            var data = HexToSpan("c3"); // 0 (double)
            AreEqual((byte)MsgFormat.True, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadBool(), null);
        }

        // --- floating point
        [Test]
        public static void Read_Eof_double()
        {
            var data = HexToSpan("cb 00 00 00 00 00 00 00 00"); // 0 (double)
            AreEqual((byte)MsgFormat.float64, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadFloat64(), "MessagePack error - unexpected EOF. was: float64(0xCB) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_float()
        {
            var data = HexToSpan("ca 00 00 00 00");             // 0 (float)
            AreEqual((byte)MsgFormat.float32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadFloat32(), "MessagePack error - unexpected EOF. was: float32(0xCA) pos: 0 (root)");
        }
        
        // --- signed integer
        [Test]
        public static void Read_Eof_int64()
        {
            var data = HexToSpan("d3 00 00 00 00 ff ff ff ff"); // 4294967295 (int64)
            AreEqual((byte)MsgFormat.int64, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt64(), "MessagePack error - unexpected EOF. was: int64(0xD3) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_int32()
        {
            var data = HexToSpan("d2 7f ff ff ff");             // 2147483647 (int32)
            AreEqual((byte)MsgFormat.int32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt32(), "MessagePack error - unexpected EOF. was: int32(0xD2) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_int16()
        {
            var data = HexToSpan("d1 7f ff");                   // 32767 (int16)
            AreEqual((byte)MsgFormat.int16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt16(), "MessagePack error - unexpected EOF. was: int16(0xD1) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_int8()
        {
            var data = HexToSpan("d0 7f");                      // 127 (int8)
            AreEqual((byte)MsgFormat.int8, data[0]);

            AssertEof(data, (ref MsgReader r) => r.ReadByte(), "MessagePack error - unexpected EOF. was: int8(0xD0) pos: 0 (root)");
        }
        
        // --- unsigend integer
        [Test]
        public static void Read_Eof_uint64()
        {
            var data = HexToSpan("cf 00 00 00 00 ff ff ff ff");
            AreEqual((byte)MsgFormat.uint64, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt64(), "MessagePack error - unexpected EOF. was: uint64(0xCF) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_uint32()
        {
            var data = HexToSpan("ce 7f ff ff ff");
            AreEqual((byte)MsgFormat.uint32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt32(), "MessagePack error - unexpected EOF. was: uint32(0xCE) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_uint16()
        {
            var data = HexToSpan("cd 7f ff");
            AreEqual((byte)MsgFormat.uint16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadInt16(), "MessagePack error - unexpected EOF. was: uint16(0xCD) pos: 0 (root)");
        }
        
        [Test]
        public static void Read_Eof_uint8()
        {
            var data = HexToSpan("cc 7f");
            AreEqual((byte)MsgFormat.uint8, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadByte(), "MessagePack error - unexpected EOF. was: uint8(0xCC) pos: 0 (root)");
        }
        
        // --- fixint +/-1
        [Test]
        public static void Read_Eof_intfix()
        {
            var data = HexToSpan("7f");                      // 127 (fixint)
            AreEqual((byte)MsgFormat.fixintPosMax, data[0]);
            
            AssertEof(data, (ref MsgReader r) => r.ReadByte(), null);
        }
        
        [Test]
        public static void Read_Eof_intfix_negative()
        {
            var data = HexToSpan("e0");                      // -32 (-fixint)
            AreEqual((byte)MsgFormat.fixintNeg, data[0]);

            AssertEof(data, (ref MsgReader r) => r.ReadInt16(), null);
        }
        
        // --- string
        [Test]
        public static void Read_Eof_strfix()
        {
            var data = HexToSpan("a1 61");
            AssertEof(data, (ref MsgReader r) => r.ReadString(), null);
        }
        
        [Test]
        public static void Read_Eof_str8()
        {
            var data = HexToSpan("d9 01 61");
            AreEqual((byte)MsgFormat.str8, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadString(), null);
        }
        
        [Test]
        public static void Read_Eof_str16()
        {
            var data = HexToSpan("da 00 01 61");
            AreEqual((byte)MsgFormat.str16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadString(), null);
        }
        
        [Test]
        public static void Read_Eof_str32()
        {
            var data = HexToSpan("db 00 00 00 01 61");
            AreEqual((byte)MsgFormat.str32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadString(), null);
        }
        
        // --- bin
        [Test]
        public static void Read_Eof_bin8()
        {
            var data = HexToSpan("c4 01 09");
            AreEqual((byte)MsgFormat.bin8, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadBin(), null);
        }
        
        [Test]
        public static void Read_Eof_bin16()
        {
            var data = HexToSpan("c5 00 01 09");
            AreEqual((byte)MsgFormat.bin16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadBin(), null);
        }
        
        [Test]
        public static void Read_Eof_bin32()
        {
            var data = HexToSpan("c6 00 00 00 01 09");
            AreEqual((byte)MsgFormat.bin32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadBin(), null);
        }
        
        // --- array
        [Test]
        public static void Read_Eof_array_fix()
        {
            var data = HexToSpan("90");
            AreEqual((byte)MsgFormat.fixarray, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadArray(out _), null);
        }
        
        [Test]
        public static void Read_Eof_array16()
        {
            var data = HexToSpan("dc 00 00");
            AreEqual((byte)MsgFormat.array16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadArray(out _), null);
        }
        
        [Test]
        public static void Read_Eof_array32()
        {
            var data = HexToSpan("dd 00 00 00 00");
            AreEqual((byte)MsgFormat.array32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadArray(out _), null);
        }
        
        // --- map
        [Test]
        public static void Read_Eof_map_fix()
        {
            var data = HexToSpan("80");
            AreEqual((byte)MsgFormat.fixmap, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadObject(out _), null);
        }
        
        [Test]
        public static void Read_Eof_map16()
        {
            var data = HexToSpan("de 00 00");
            AreEqual((byte)MsgFormat.map16, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadObject(out _), null);
        }
        
        [Test]
        public static void Read_Eof_map32()
        {
            var data = HexToSpan("df 00 00 00 00");
            AreEqual((byte)MsgFormat.map32, data[0]);
            AssertEof(data, (ref MsgReader r) => r.ReadObject(out _), null);
        }
    }
}