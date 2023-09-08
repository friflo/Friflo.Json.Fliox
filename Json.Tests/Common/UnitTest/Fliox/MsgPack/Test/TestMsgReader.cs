using System;
using Friflo.Json.Fliox.MsgPack;
using NUnit.Framework;
using static NUnit.Framework.Assert;

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
            ReadOnlySpan<byte> data = new byte[] { 207,  0,0,0,0,  255,255,255,255 }; // 4294967295 (uint64)
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
            ReadOnlySpan<byte> data = new byte[] { 4 }; // 4 (+fixint)
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
            ReadOnlySpan<byte> data = new byte[] { 252 }; // -4 (-fixint)
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
                ReadOnlySpan<byte> data = new byte[] { 194 }; // false
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsFalse(x);
            } {
                ReadOnlySpan<byte> data = new byte[] { 195 }; // true
                var reader = new MsgReader(data);
                var x = reader.ReadBool();
                IsTrue(x);
            } {
                ReadOnlySpan<byte> data = new byte[] { 4 }; // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadBool();
                AreEqual("MessagePack error - expect boolean. was: +fixint(0x4) pos: 0 (root)", reader.Error);
            }
        }
        
        [Test]
        public static void Read_String()
        {
            {
                ReadOnlySpan<byte> data = new byte[] { 163, 97, 98, 99 }; // "abc" (fixstr)
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("abc", x);
            }
            {
                ReadOnlySpan<byte> data = new byte[] {
                    0xd9, 10, 
                    48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                ReadOnlySpan<byte> data = new byte[] {
                    0xda, 0, 10, 
                    48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                ReadOnlySpan<byte> data = new byte[] {
                    0xdb, 0, 0, 0, 10, 
                    48, 49, 50, 51, 52, 53, 54, 55, 56, 57 };
                var reader = new MsgReader(data);
                var x = reader.ReadString();
                AreEqual("0123456789", x);
            }
            {
                ReadOnlySpan<byte> data = new byte[] { 4 }; // 4 (+fixint)
                var reader = new MsgReader(data);
                reader.ReadString();
                AreEqual("MessagePack error - expect string or null. was: +fixint(0x4) pos: 0 (root)", reader.Error);
            }
        }
        
    }
}