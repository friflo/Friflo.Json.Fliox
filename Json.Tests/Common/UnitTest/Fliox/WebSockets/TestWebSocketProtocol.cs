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
            var readBuffer = new byte[1000];
            {
                var len = await WriteRead (false, 0x100000, 4096, readBuffer); // ensure writer create no message fragmentation
                AreEqual(131242, len);
            }
            {
                var len = await WriteRead (false, 4096, 4096, readBuffer);
                AreEqual(131356, len);
            }
            {
                var len = await WriteRead (false, 80, 4096, readBuffer);
                AreEqual(134508, len);
            }
        }
        
        /// <summary> Test frame writer and reader - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsMasking()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsMasking); }
        private static async Task AssertWebSocketsMasking() {
            var readBuffer = new byte[1000];
            {
                var len = await WriteRead (true, 0x100000, 4096, readBuffer); // ensure writer create no message fragmentation
                AreEqual(131266, len);
            }
            {
                var len = await WriteRead (true, 4096, 4096, readBuffer);
                AreEqual(131500, len);
            }
            {
                var len = await WriteRead (true, 80, 4096, readBuffer);
                AreEqual(141088, len);
            }
        }
        
        /// <summary> Test reader getting always only a single byte from stream - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsReadSingleByte()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsReadSingleByte); }
        private static async Task AssertWebSocketsReadSingleByte() {
            var readBuffer = new byte[1000];
            {
                var len = await WriteRead (true, 0x100000, 1, readBuffer); // ensure writer create no message fragmentation
                AreEqual(131266, len);
            }
            {
                var len = await WriteRead (true, 4096, 1, readBuffer);
                AreEqual(131500, len);
            }
            {
                var len = await WriteRead (true, 80, 1, readBuffer);
                AreEqual(141088, len);
            }
        }
        
        /// <summary> Use a dataBuffer with one element in <see cref="FrameProtocolReader.ReadFrame"/> - masking <b>enabled</b> </summary>
        [Test]      public void  TestWebSocketsSmallBuffer()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsSmallBuffer); }
        private static async Task AssertWebSocketsSmallBuffer() {
            var readBuffer = new byte[1];
            {
                var len = await WriteRead (true, 0x100000, 4096, readBuffer); // ensure writer create no message fragmentation
                AreEqual(131266, len);
            }
            {
                var len = await WriteRead (true, 4096, 4096, readBuffer);
                AreEqual(131500, len);
            }
            {
                var len = await WriteRead (true, 80, 4096, readBuffer);
                AreEqual(141088, len);
            }
        }

        private static async Task<long> WriteRead(bool mask, int writerSize, int readerSize, byte[] readBuffer)
        {
            var writer = new FrameProtocolWriter(mask, writerSize);
            
            var stream = new MemoryStream();
            await Write (writer, stream, "Test-1");
            await Write (writer, stream, "");
            
            var str126 = new string('a', 126);
            await Write (writer, stream, str126);
            
            var str0xffff = $"{new string('b', 0xffff)}";
            await Write (writer, stream, str0xffff);
            
            var str0x10000 = $"{new string('c', 0x10000)}";
            await Write (writer, stream, str0x10000);
            
            await writer.CloseAsync(stream, WebSocketCloseStatus.NormalClosure, "test finished", CancellationToken.None);

            var messageBuffer   = new MemoryStream();
            var reader          = new FrameProtocolReader(readerSize);
            stream.Position     = 0;

            var result  = await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual("Test-1", result);
            
            result      = await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual("", result);
            
            result      = await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual(str126, result);
            
            result      = await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual(str0xffff, result);
            
            result      = await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual(str0x10000, result);
            
            await Read(reader, stream, readBuffer, messageBuffer);
            AreEqual(WebSocketState.CloseReceived,          reader.SocketState);
            AreEqual(WebSocketMessageType.Close,            reader.MessageType);
            AreEqual(WebSocketCloseStatus.NormalClosure,    reader.CloseStatus);
            AreEqual("test finished",                       reader.CloseStatusDescription);
            
            AreEqual(stream.Length, stream.Position);
            
            return stream.Position;
        }
        
        private static async Task           Write(FrameProtocolWriter writer, Stream stream, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await writer.WriteFrame(stream, bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private static async Task<string>   Read(FrameProtocolReader reader, Stream stream, byte[] buffer, MemoryStream messageBuffer)
        {
            messageBuffer.Position = 0;
            messageBuffer.SetLength(0);
            while (true) {
                await reader.ReadFrame(stream, buffer, CancellationToken.None);
                
                // if (reader.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("expect text message");
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
                var dataBuffer  = new byte[4096];
                AreEqual(WebSocketState.Open,                       reader.SocketState);
                
                var socketState = await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
                
                IsTrue  (reader.EndOfMessage);
                AreEqual(WebSocketState.Closed,                     socketState);
                AreEqual(WebSocketState.Closed,                     reader.SocketState);
                AreEqual(WebSocketMessageType.Close,                reader.MessageType);
                AreEqual(WebSocketCloseStatus.EndpointUnavailable,  reader.CloseStatus);
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
            var dataBuffer  = new byte[4096];
            
            var socketState = await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
            
            IsTrue  (reader.EndOfMessage);
            AreEqual(WebSocketState.CloseReceived,          socketState);
            AreEqual(WebSocketState.CloseReceived,          reader.SocketState);
            AreEqual(WebSocketCloseStatus.NormalClosure,    reader.CloseStatus);
            AreEqual(WebSocketMessageType.Close,            reader.MessageType);
            AreEqual("test connection close",               reader.CloseStatusDescription);
        }
        
        /// <summary> Test closing a reader by control frame <see cref="Opcode.ConnectionClose"/> without close status (e.g. 1000)</summary>
        [Test]      public void  TestWebSocketsCloseConnectionNoStatus()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsTestCloseConnectionNoStatus); }
        private static async Task AssertWebSocketsTestCloseConnectionNoStatus() {
            var writer = new FrameProtocolWriter(true);
            var stream = new MemoryStream();
            
            await writer.CloseAsync(stream, null, "", CancellationToken.None);

            stream.Position = 0;
            var reader      = new FrameProtocolReader();
            var dataBuffer  = new byte[4096];
            
            var socketState = await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
            
            IsTrue  (reader.EndOfMessage);
            AreEqual(WebSocketState.CloseReceived,          socketState);
            AreEqual(WebSocketState.CloseReceived,          reader.SocketState);
            AreEqual(WebSocketCloseStatus.NormalClosure,    reader.CloseStatus);
            AreEqual(WebSocketMessageType.Close,            reader.MessageType);
            AreEqual("",                                    reader.CloseStatusDescription);
        }
        
        [Test]      public void  TestWebSocketsPerf()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsPerf); }
        private static async Task AssertWebSocketsPerf() {
            var writer          = new FrameProtocolWriter(true, 4094); // mask == false  =>  4 x faster by Buffer.BlockCopy() in reader
            var reader          = new FrameProtocolReader(4094);
            var readBuffer      = new byte[4094];
            var stream          = new MemoryStream();
            var payloadSize     = 208;
            var payload         = Encoding.UTF8.GetBytes(new string('x', payloadSize));
            // payload          = Encoding.UTF8.GetBytes("0123456789abcdef");
            
            var count = 10; // 1_000_000;
            for (int n = 0; n < count; n++) {
                stream.Position = 0;
                await writer.WriteFrame(stream, payload, WebSocketMessageType.Text, true, CancellationToken.None);
                
                stream.Position = 0;
                await reader.ReadFrame(stream, readBuffer, CancellationToken.None);
                if (reader.ByteCount != payload.Length) throw new InvalidOperationException($"expect {payload.Length}, was: {reader.ByteCount}");
            }
            Console.WriteLine($"ProcessedByteCount: {reader.ProcessedByteCount}");
        }
    }
}