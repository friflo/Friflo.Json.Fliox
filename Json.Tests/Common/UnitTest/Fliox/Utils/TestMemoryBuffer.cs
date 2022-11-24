// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Friflo.Json.Fliox.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Utils
{
    public static class TestMemoryBuffer
    {
        private static readonly byte[] Hello = Encoding.UTF8.GetBytes("hello");
        
        [Test]
        public static void TestMemoryBufferCapacity1() {
            MemoryStream ms = new MemoryStream();
            ms.Write(Hello);
            ms.Position = 0;
            
            var buffer = new MemoryBuffer(1);
            var result = ReadString(buffer, ms);

            AreEqual("hello", result);
            AreEqual(8, buffer.Capacity);
        }
        
        [Test]
        public static void TestMemoryBufferStart() {
            MemoryStream ms = new MemoryStream();
            ms.Write(Hello);
            ms.Position = 0;
            
            var buffer = new MemoryBuffer(10) { Position = 9 };
            buffer.SetMessageStart();
            
            var result = ReadString(buffer, ms);

            AreEqual("hello", result);
            AreEqual(20, buffer.Capacity);
        }

        private static string ReadString(MemoryBuffer buffer, Stream stream) {
            int read;
            while ((read = stream.Read(buffer.GetBuffer(), buffer.Position, buffer.Capacity - buffer.Position)) > 0)
            {
                buffer.Position += read;
                if (buffer.Position == buffer.Capacity) {
                    buffer.SetCapacity(2 * buffer.Capacity);
                }
            }
            return Encoding.UTF8.GetString(buffer.GetBuffer(), buffer.MessageStart, buffer.MessageLength);
        }
    }
}