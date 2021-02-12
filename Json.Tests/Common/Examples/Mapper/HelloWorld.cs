using System;
using Friflo.Json.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
#if !UNITY_5_3_OR_NEWER  // no clean up of native containers for Unity/JSON_BURST
    
    public class TestHelloWorld
    {
        class Message {
            public string say;
            public string to;
        }
        
        [Test]
        public void HelloWorldReader() {
            var r = new JsonReader(new TypeStore());
            var msg = r.Read<Message>(@"{""say"": ""Hello 👋"", ""to"": ""World""}");
            Console.WriteLine($"Output: {msg.say}, {msg.to}");
            // Output: Hello 👋, World
        }
        
        [Test]
        public void HelloWorldWriter() {
            var w = new JsonWriter(new TypeStore());
            w.Write(new Message {say = "Hello 👋", to = "World"});
            Console.WriteLine($"Output: {w.Output}");
            // Output: {"say":"Hello 👋","to":"World"}
        }

    }
#endif
}