using System;
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
        [Test]      public void  TestWebSocketsWriteRead()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsWriteRead); }
        private static async Task AssertWebSocketsWriteRead() {
            // await WriteRead (false, 0x100000); // ensure no message fragmentation
            // await WriteRead (false, 4096);
            {
                var len = await WriteRead (false, 80);
                AreEqual(146, len);
            }
        }
        
        [Test]      public void  TestWebSocketsWriteReadMask()       { SingleThreadSynchronizationContext.Run(AssertWebSocketsWriteReadMask); }
        private static async Task AssertWebSocketsWriteReadMask() {
            {
                var len = await WriteRead (true, 80);
                AreEqual(162, len);
            }
        }

        private static async Task<long> WriteRead(bool mask, int bufferSize)
        {
            var writer = new FrameProtocolWriter(mask, bufferSize);
            
            var stream = new MemoryStream();
            await Write (writer, stream, "Test-1");
            await Write (writer, stream, "Test-2");
            
            var str126 = new string('a', 126);
            await Write (writer, stream, str126);
            
            /* var str0xffff = $"{new string('b', 0xffff)}";
            await Write (writer, stream, str0xffff);
            
            var str0x10000 = $"{new string('c', 0x10000)}";
            await Write (writer, stream, str0x10000); */

            var reader = new FrameProtocolReader();
            stream.Position = 0;
            
            var result =  await Read(reader, stream);
            AreEqual("Test-1", result);
            
            result =  await Read(reader, stream);
            AreEqual("Test-2", result);
            
            result =  await Read(reader, stream);
            AreEqual(str126, result);
            
            /* result =  await Read(reader, stream);
            AreEqual(str0xffff, result);
            
            result =  await Read(reader, stream);
            AreEqual(str0x10000, result); */
            
            AreEqual(stream.Length, stream.Position);
            
            return stream.Position;
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