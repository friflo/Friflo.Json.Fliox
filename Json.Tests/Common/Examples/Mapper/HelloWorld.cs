using System;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    public class TestHelloWorld
    {
        class Message {
            public string say;
            public string to;
        }
        
        [Test]
        public void HelloWorldReader() {
            var m = new ObjectMapper();
            var msg = m.Read<Message>(@"{""say"": ""Hello 👋"", ""to"": ""World""}");
            Console.WriteLine($"Output: {msg.say}, {msg.to}");
            // Output: Hello 👋, World
        }
        
        [Test]
        public void HelloWorldWriter() {
            var m = new ObjectMapper();
            var json = m.Write(new Message {say = "Hello 👋", to = "World"});
            Console.WriteLine($"Output: {json}");
            // Output: {"say":"Hello 👋","to":"World"}
        }

    }
}