// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Friflo.Json.Fliox;
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
            AreEqual(0,         buffer.BufferVersion);
            
            var result = ReadString(buffer, ms);

            AreEqual("hello", result);
            AreEqual(8,         buffer.Capacity);   // capacity is doubled 3 times
            AreEqual(5,         buffer.Position);
            AreEqual(3,         buffer.BufferVersion);
        }
        
        [Test]
        public static void TestMemoryBufferStart() {
            MemoryStream ms = new MemoryStream();
            ms.Write(Hello);
            ms.Position = 0;
            
            var buffer = new MemoryBuffer(10) { Position = 9 };
            AreEqual(0,         buffer.BufferVersion);
            
            var result = ReadString(buffer, ms);

            AreEqual("hello", result);
            AreEqual(10,        buffer.Capacity);       // capacity is sufficient to store the message
            AreEqual(5,         buffer.Position);
            AreEqual(1,         buffer.BufferVersion);
        }

        private static string ReadString(MemoryBuffer buffer, Stream stream) {
            buffer.SetMessageStart();
            int read;
            while ((read = stream.Read(buffer.GetBuffer(), buffer.Position, buffer.Capacity - buffer.Position)) > 0)
            {
                buffer.Position += read;
                if (buffer.Position < buffer.Capacity)
                    continue;
                buffer.AddReadBuffer();
            }
            return Encoding.UTF8.GetString(buffer.GetBuffer(), buffer.MessageStart, buffer.MessageLength);
        }
        
        [Test]
        public static void TestMemoryBufferAddJsonValue()
        {
            {
                var buffer = new MemoryBuffer(2);
                AreEqual(0,         buffer.Position);
                AreEqual(0,         buffer.BufferVersion);
                
                var value   = new JsonValue("ab");
                var result  = value.AddToBuffer(buffer);
                AreEqual("ab",      result.AsString());
                AreEqual(2,         buffer.Capacity);
                AreEqual(2,         buffer.Position);
                AreEqual(0,         buffer.BufferVersion);
                
                value   = new JsonValue("c");
                result  = value.AddToBuffer(buffer);
                AreEqual("c",       result.AsString());
                AreEqual(2,         buffer.Capacity);
                AreEqual(1,         buffer.Position);
                AreEqual(1,         buffer.BufferVersion);
                
                value   = new JsonValue("12");
                result  = value.AddToBuffer(buffer);
                AreEqual("12",      result.AsString());
                AreEqual(4,         buffer.Capacity);
                AreEqual(2,         buffer.Position);
                AreEqual(2,         buffer.BufferVersion);
                
                value   = new JsonValue("xyz");
                result  = value.AddToBuffer(buffer);
                AreEqual("xyz",     result.AsString());
                AreEqual(8,         buffer.Capacity);
                AreEqual(3,         buffer.Position);
                AreEqual(3,         buffer.BufferVersion);
            }
            {
                var buffer = new MemoryBuffer(2);
                AreEqual(0,         buffer.BufferVersion);
                
                var value   = new JsonValue("abc");
                var result  = value.AddToBuffer(buffer);
                AreEqual("abc",     result.AsString());
                AreEqual(4,         buffer.Capacity);
                AreEqual(3,         buffer.Position);
                AreEqual(1,         buffer.BufferVersion);
            }
            {
                var buffer = new MemoryBuffer(2);
                AreEqual(0,         buffer.BufferVersion);
                
                var value   = new JsonValue("12345");
                var result  = value.AddToBuffer(buffer);
                AreEqual("12345",   result.AsString());
                AreEqual(5,         buffer.Capacity);
                AreEqual(5,         buffer.Position);
                AreEqual(1,         buffer.BufferVersion);
            }
        }
    }
}