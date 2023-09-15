using System;
using Friflo.Json.Fliox.MsgPack;
using Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Reader;
using Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.MsgPack.MsgPackUtils;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Writer.KeyType;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.MsgPack.Test.Writer
{
    public enum KeyType
    {
        FixStr,
        Str8,
    }
    
    public static class TestMsgWriter
    {
        [Test]
        public static void Write_Child_map()
        {
            var sample = new Sample { x = int.MaxValue, child = new Child { y = 42 } };
            var writer = new MsgWriter(new byte[10], true);
            writer.WriteMsg(ref sample);
            AreEqual(18, writer.Length);
            AreEqual(HexNorm("82 A1 78 CE 7F FF FF FF A5 63 68 69 6C 64 81 A1 79 2A"), writer.DataHex);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        private const               long    X       = 0x78;
        private static  readonly    byte[]  XArr    = new byte[] { (byte)'x'};
        
        // --- string
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_strfix(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            int count = 0;
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, "abc", ref count);    break;
                case Str8:      writer.WriteKeyString (XArr, "abc", ref count);    break;
            }
            writer.WriteMapFixEnd(0, 1);
            AreEqual(1, count);
            
            AreEqual(HexNorm("81 A1 78 A3 61 62 63"), writer.DataHex);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
       
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str8(KeyType keyType)
        {
            var val = "_123456789_123456789_123456789_123456789";
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            int count = 0;
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val, ref count);      break;
                case Str8:      writer.WriteKeyString (XArr, val, ref count);      break;
            }
            writer.WriteMapFixEnd(0, 1);
            AreEqual(1, count);
            
            AreEqual(HexNorm("81 A1 78 D9 28 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39"), writer.DataHex);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str16(KeyType keyType)
        {
            var val = new string('a', 300);
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            int count = 0;
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val, ref count);      break;
                case Str8:      writer.WriteKeyString (XArr, val, ref count);      break;
            }
            writer.WriteMapFixEnd(0, 1);
            AreEqual(1, count);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out _);
            reader.ReadKey();
            var x = reader.ReadString();
            AreEqual(300, x.Length);
            AreEqual(val, x);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_str32(KeyType keyType)
        {
            var val = new string('a', 70000);
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            int count = 0;
            switch (keyType) {
                case FixStr:    writer.WriteKeyString (1, X, val, ref count);      break;
                case Str8:      writer.WriteKeyString (XArr, val, ref count);      break;
            }
            writer.WriteMapFixEnd(0, 1);
            AreEqual(1, count);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out _);
            reader.ReadKey();
            var x = reader.ReadString();
            AreEqual(70000, x.Length);
            AreEqual(val, x);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [TestCase(FixStr)] [TestCase(Str8)]
        public static void Write_key_bool(KeyType keyType)
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteMapFixBegin();
            switch (keyType) {
                case FixStr:    writer.WriteKeyBool (1, X, true);      break;
                case Str8:      writer.WriteKeyBool (XArr, true);      break;
            }
            writer.WriteMapFixEnd(0, 1);
            
            AreEqual(HexNorm("81 A1 78 C3"), writer.DataHex);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_keyfix()
        {
            var writer = new MsgWriter(new byte[10], false);
            var count = 0;
            writer.WriteMapFixBegin();

            writer.WriteKey(0, 0x0000_0000_0000_0000, ref count);
            writer.WriteNull();
            
            writer.WriteKey(1, 0x0000_0000_0000_0061, ref count);
            writer.WriteNull();
            
            writer.WriteKey(2, 0x0000_0000_0000_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(3, 0x0000_0000_0063_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(4, 0x0000_0000_6463_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(5, 0x0000_0065_6463_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(6, 0x0000_6665_6463_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(7, 0x0067_6665_6463_6261, ref count);
            writer.WriteNull();
            
            writer.WriteKey(8, 0x6867_6665_6463_6261, ref count);
            writer.WriteNull();
            
            writer.WriteMapFixEnd(0, count);
            AreEqual(9, count);
            AreEqual(HexNorm("89 A0 C0 A1 61 C0 A2 61 62 C0 A3 61 62 63 C0 A4 61 62 63 64 C0 A5 61 62 63 64 65 C0 A6 61 62 63 64 65 66 C0 A7 61 62 63 64 65 66 67 C0 A8 61 62 63 64 65 66 67 68 C0"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out int length);
            AreEqual(9, length);
            
            AreEqual(0, reader.ReadKey());
            AreEqual("", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x61, reader.ReadKey());
            AreEqual("a", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x6261, reader.ReadKey());
            AreEqual("ab", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x63_6261, reader.ReadKey());
            AreEqual("abc", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x6463_6261, reader.ReadKey());
            AreEqual("abcd", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x65_6463_6261, reader.ReadKey());
            AreEqual("abcde", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x6665_6463_6261, reader.ReadKey());
            AreEqual("abcdef", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x67_6665_6463_6261, reader.ReadKey());
            AreEqual("abcdefg", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x6867_6665_6463_6261, reader.ReadKey());
            AreEqual("abcdefgh", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_key8()
        {
            var writer = new MsgWriter(new byte[10], true);
            var count = 0;
            writer.WriteMapFixBegin();

            writer.WriteKeyString("".String2Span(), null, ref count);
            writer.WriteKeyString("abcdefg".String2Span(), null, ref count);
            writer.WriteKeyString("_123456789_123456789_123456789_123456789".String2Span(), null, ref count);
            writer.WriteMapFixEnd(0, 3);
            
            AreEqual(HexNorm("83 A0 C0 A7 61 62 63 64 65 66 67 C0 D9 28 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 5F 31 32 33 34 35 36 37 38 39 C0"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadObject(out int length);
            AreEqual(3, length);
            
            AreEqual(0, reader.ReadKey());
            AreEqual("", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x0067_6665_6463_6261, reader.ReadKey());
            AreEqual("abcdefg", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            AreEqual(0x3736_3534_3332_315f, reader.ReadKey());
            AreEqual("_123456789_123456789_123456789_123456789", reader.KeyName.DataString());
            IsNull(reader.ReadString());
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        
        // --- array
        [Test]
        public static void Write_array_fix()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(1);
            writer.WriteInt32(42);
            
            AreEqual(HexNorm("91 2A"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(1, length);
            var item = reader.ReadInt32();
            AreEqual(42, item);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_array_16()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(16);
            for (int n = 0; n < 16; n++) { 
                writer.WriteInt32(n);
            }
            
            AreEqual(HexNorm("DC 00 10 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F"), writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(16, length);
            for (int n = 0; n < length; n++) {
                var item = reader.ReadInt32();
                AreEqual(n, item);
            }
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_array_32()
        {
            var writer = new MsgWriter(new byte[10], false);
            writer.WriteArray(0x10000);
            for (int n = 0; n < 0x10000; n++) { 
                writer.WriteInt32(n);
            }
            AreEqual(196229, writer.Data.Length);
            
            var reader = new MsgReader(writer.Data);
            reader.ReadArray(out int length);
            AreEqual(0x10000, length);
            for (int n = 0; n < length; n++) {
                var item = reader.ReadInt32();
                if (item != n) Fail("expected: {n}, was: {item}");
            }
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        // --- bin (byte array)
        [Test]
        public static void Write_bin_null()
        {
            var span    = new ReadOnlySpan<byte>();
            var writer  = new MsgWriter(new byte[10], false);
            writer.WriteBin(span);
            AreEqual("C0", writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            var result = reader.ReadBin();
            IsTrue(result == null);
            AreEqual(writer.Data.Length, reader.Pos);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_bin8()
        {
            var hex     = "0A"; 
            var span    = HexToSpan(hex);
            var writer  = new MsgWriter(new byte[10], false);
            writer.WriteBin(span);
            AreEqual("C4 01 0A", writer.DataHex);
            
            var reader = new MsgReader(writer.Data);
            var result = reader.ReadBin();
            AreEqual(hex, result.DataHex());
            AreEqual(writer.Data.Length, reader.Pos);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_bin16()
        {
            var data    = new byte[300];
            data[0]     = 10;
            data[299]   = 11;
            var span    = new ReadOnlySpan<byte>(data);
            var writer  = new MsgWriter(new byte[10], false);
            writer.WriteBin(span);
            AreEqual(303, writer.Data.Length);
            
            var reader = new MsgReader(writer.Data);
            var result = reader.ReadBin();
            IsTrue(span.SequenceEqual(result));
            AreEqual(writer.Data.Length, reader.Pos);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
        
        [Test]
        public static void Write_bin32()
        {
            var data        = new byte[0x10001];
            data[0]         = 10;
            data[0x10000]   = 11;
            var span    = new ReadOnlySpan<byte>(data);
            var writer  = new MsgWriter(new byte[10], false);
            writer.WriteBin(span);
            AreEqual(0x10006, writer.Data.Length);
            
            var reader = new MsgReader(writer.Data);
            var result = reader.ReadBin();
            IsTrue(span.SequenceEqual(result));
            AreEqual(writer.Data.Length, reader.Pos);
            
            TestMsgReader.AssertSkip(writer.Data);
        }
    }
}