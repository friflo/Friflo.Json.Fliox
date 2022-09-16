using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.WebSockets;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.WebSockets
{
    public class TestWebSocketProtocol
    {
        /// <summary> Test frame writer and reader - masking <b>disabled</b> </summary>
        [Test]      public void  TestWebSocketsNoMasking()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsNoMasking); }
        private static async Task AssertWebSocketsNoMasking() {
            {
                var len = await WriteRead (false, 0x100000, 4096); // ensure writer create no message fragmentation
                AreEqual(131231, len);
            }
            {
                var len = await WriteRead (false, 4096, 4096);
                AreEqual(131345, len);
            }
            {
                var len = await WriteRead (false, 80, 4096);
                AreEqual(134497, len);
            }
        }
        
        /// <summary> Test frame writer and reader - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsMasking()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsMasking); }
        private static async Task AssertWebSocketsMasking() {
            {
                var len = await WriteRead (true, 0x100000, 4096); // ensure writer create no message fragmentation
                AreEqual(131251, len);
            }
            {
                var len = await WriteRead (true, 4096, 4096);
                AreEqual(131485, len);
            }
            {
                var len = await WriteRead (true, 80, 4096);
                AreEqual(141073, len);
            }
        }
        
        /// <summary> Test reader getting always only a single byte from stream - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsReadSingleByte()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsReadSingleByte); }
        private static async Task AssertWebSocketsReadSingleByte() {
            {
                var len = await WriteRead (true, 0x100000, 1); // ensure writer create no message fragmentation
                AreEqual(131251, len);
            }
            {
                var len = await WriteRead (true, 4096, 1);
                AreEqual(131485, len);
            }
            {
                var len = await WriteRead (true, 80, 1);
                AreEqual(141073, len);
            }
        }
        
        /// <summary> Use a dataBuffer with one element in <see cref="FrameProtocolReader.ReadFrame"/> - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsSmallBuffer()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsSmallBuffer); }
        private static async Task AssertWebSocketsSmallBuffer() {
            {
                var len = await WriteRead (true, 0x100000, 4096, 1); // ensure writer create no message fragmentation
                AreEqual(131251, len);
            }
            {
                var len = await WriteRead (true, 4096, 4096, 1);
                AreEqual(131485, len);
            }
            {
                var len = await WriteRead (true, 80, 4096, 1);
                AreEqual(141073, len);
            }
        }

        private static async Task<long> WriteRead(bool mask, int writerSize, int readerSize, int bufferSize = 1000)
        {
            var writer = new FrameProtocolWriter(mask, writerSize);
            
            var stream = new MemoryStream();
            await Write (writer, stream, "Test-1");
            await Write (writer, stream, "Test-2");
            
            var str126 = new string('a', 126);
            await Write (writer, stream, str126);
            
            var str0xffff = $"{new string('b', 0xffff)}";
            await Write (writer, stream, str0xffff);
            
            var str0x10000 = $"{new string('c', 0x10000)}";
            await Write (writer, stream, str0x10000);

            var buffer          = new byte[bufferSize];
            var messageBuffer   = new MemoryStream();
            var reader = new FrameProtocolReader(readerSize);
            stream.Position = 0;

            var result  = await Read(reader, stream, buffer, messageBuffer);
            AreEqual("Test-1", result);
            
            result      = await Read(reader, stream, buffer, messageBuffer);
            AreEqual("Test-2", result);
            
            result      = await Read(reader, stream, buffer, messageBuffer);
            AreEqual(str126, result);
            
            result      = await Read(reader, stream, buffer, messageBuffer);
            AreEqual(str0xffff, result);
            
            result      = await Read(reader, stream, buffer, messageBuffer);
            AreEqual(str0x10000, result);
            
            AreEqual(stream.Length, stream.Position);
            
            return stream.Position;
        }
        
        private static async Task           Write(FrameProtocolWriter writer, Stream stream, string message)
        {
            var bytes       = Encoding.UTF8.GetBytes(message);
            var dataBuffer  = new ArraySegment<byte>(bytes); 
            await writer.WriteAsync(stream, dataBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private static async Task<string>   Read(FrameProtocolReader reader, Stream stream, byte[] buffer, MemoryStream messageBuffer)
        {
            messageBuffer.Position = 0;
            var dataBuffer  = new ArraySegment<byte>(buffer);
            while (true) {
                await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
                
                if (reader.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("expect text message");
                var byteCount   = reader.ByteCount;
                messageBuffer.Write(buffer, 0, byteCount);
                if (!reader.EndOfMessage)
                    continue;
                var targetArray = messageBuffer.GetBuffer();
                var targetLen   = (int)messageBuffer.Length;
                return Encoding.UTF8.GetString(targetArray, 0, targetLen);
            }
        }

        /// <summary> Test closing a reader from a closed stream </summary>
        [Test]      public void  TestWebSocketsCloseStream()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsCloseStream); }
        private static async Task AssertWebSocketsCloseStream() {
            var writer = new FrameProtocolWriter(true);
            var stream = new MemoryStream();
            
            await Write (writer, stream, "hi");
            
            for (var n = stream.Length - 1; n >= 0; n--) {
                stream.SetLength(n); // test a closed stream with various lengths
                stream.Position = 0;
                
                var reader      = new FrameProtocolReader();
                var dataBuffer  = new ArraySegment<byte>(new byte[4094]);
                AreEqual(WebSocketState.Open,                       reader.SocketState);
                
                bool success    = await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
                
                IsFalse (success);
                IsTrue  (reader.EndOfMessage);
                AreEqual(WebSocketState.Closed,                     reader.SocketState);
                AreEqual(WebSocketCloseStatus.EndpointUnavailable,  reader.CloseStatus);
                AreEqual(WebSocketMessageType.Close,                reader.MessageType);
                AreEqual("stream closed",                           reader.CloseStatusDescription);
                
                try {
                    // read from closed reader
                    await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
                    
                    Debug.Fail("expect exception");
                } catch (Exception e) {
                    AreEqual("reader already closed", e.Message);
                }
            }
        }
        
        /// <summary> Test closing a reader by control frame <see cref="Opcode.ConnectionClose"/> </summary>
        [Test]      public void  TestWebSocketsCloseConnection()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsTestCloseConnection); }
        private static async Task AssertWebSocketsTestCloseConnection() {
            var writer = new FrameProtocolWriter(true);
            var stream = new MemoryStream();
            
            await writer.CloseAsync(stream, WebSocketCloseStatus.NormalClosure, "test connection close", CancellationToken.None);

            stream.Position = 0;
            var reader      = new FrameProtocolReader();
            var dataBuffer  = new ArraySegment<byte>(new byte[4094]);
            
            bool success    = await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
            
            IsFalse (success);
            IsTrue  (reader.EndOfMessage);
            AreEqual(WebSocketState.CloseReceived,          reader.SocketState);
            AreEqual(WebSocketCloseStatus.NormalClosure,    reader.CloseStatus);
            AreEqual(WebSocketMessageType.Close,            reader.MessageType);
            AreEqual("test connection close",               reader.CloseStatusDescription);
        }
    }
}