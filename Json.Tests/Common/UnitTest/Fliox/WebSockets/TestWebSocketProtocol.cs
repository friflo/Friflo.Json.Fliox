using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Remote.WebSockets;
using Friflo.Json.Fliox.Hub.Threading;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.WebSockets
{
    public class TestWebSocketProtocol
    {
        [Test]      public void  TestWebSocketsWriteRead()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsWriteRead); }
        private static async Task AssertWebSocketsWriteRead() {
            await WriteRead (false, 80);
        }
        
        // [Test]      public void  TestWebSocketsWriteReadMask()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsWriteReadMask); }
        private static async Task AssertWebSocketsWriteReadMask() {
            await WriteRead (true, 80);
        }

        private static async Task WriteRead(bool mask, int bufferSize)
        {
            var writer = new FrameProtocolWriter(mask, bufferSize);
            
            var stream = new MemoryStream();
            await Write (writer, stream, "Test-1");
            await Write (writer, stream, "Test-2");
            var str126 = $"B{new string('x', 124)}E";
            await Write (writer, stream, str126);
            
            var reader = new FrameProtocolReader();
            stream.Position = 0;
            
            var result =  await Read(reader, stream);
            Assert.AreEqual("Test-1", result);
            
            result =  await Read(reader, stream);
            Assert.AreEqual("Test-2", result);
            
            result =  await Read(reader, stream);
            Assert.AreEqual(str126, result);
        }
        
        private static async Task           Write(FrameProtocolWriter writer, Stream stream, string message)
        {
            var bytes       = Encoding.UTF8.GetBytes(message);
            var dataBuffer  = new ArraySegment<byte>(bytes); 
            await writer.WriteAsync(stream, dataBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private static async Task<string>   Read(FrameProtocolReader reader, Stream stream)
        {
            var bytes       = new byte[1000];
            var dataBuffer  = new ArraySegment<byte>(bytes);
            var sb          = new StringBuilder();
            while (true) {
                await reader.ReadFrame(stream, dataBuffer, CancellationToken.None);
                
                if (reader.MessageType != WebSocketMessageType.Text) throw new InvalidOperationException("expect text message");
                var byteCount   = reader.ByteCount;
                var payload     = Encoding.UTF8.GetString(bytes, 0, byteCount);
                sb.Append(payload);
                if (reader.EndOfMessage)
                    return sb.ToString();
            }
        }
    }
}