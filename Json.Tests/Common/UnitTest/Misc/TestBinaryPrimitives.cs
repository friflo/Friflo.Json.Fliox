using System;
using System.Buffers.Binary;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public static class TestBinaryPrimitives
    {
        [Test]
        public static void TestPrimitiveRead() {
            ReadOnlySpan<byte> span = new byte[] { 1, 2, 3, 4 };
            var int32 = BinaryPrimitives.ReadInt32BigEndian(span);
            Assert.AreEqual(0x01020304, int32);
        }
    }
}