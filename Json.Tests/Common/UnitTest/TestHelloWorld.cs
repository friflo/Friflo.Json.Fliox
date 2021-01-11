using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed;
using Friflo.Json.Managed.Prop;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest
{
#if !UNITY_5_3_OR_NEWER  // no clean up of native containers for Unity/JSON_BURST
    
    public class TestHelloWorld
    {
        [Test]
        public void HelloWorldParser() {
            string say = "", to = "";
            var p = new JsonParser();
            p.InitParser(new Bytes (@"{""say"": ""Hello"", ""to"": ""World 🌎""}"));
            p.NextEvent();
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if (p.UseMemberStr(ref i, "say"))  { say = p.value.ToString(); }
                if (p.UseMemberStr(ref i, "to"))   { to =  p.value.ToString(); }
            }
            Console.WriteLine($"{say}, {to}"); // your console may not support Unicode
        }
        
        [Test]
        public void HelloWorldSerializer() {
            var s = new JsonSerializer();
            s.InitSerializer();
            s.ObjectStart();
            s.MemberStr("say", "Hello");
            s.MemberStr("to",  "World 🌎");
            s.ObjectEnd();
            Console.WriteLine(s.dst.ToString()); // your console may not support Unicode
        }

        class Message {
            public string say;
            public string to;
        }
        
        [Test]
        public void HelloWorldReader() {
            var r = new JsonReader(new PropType.Store());
            var msg = r.Read<Message>(new Bytes (@"{""say"": ""Hello 👋"", ""to"": ""World""}"));
            Console.WriteLine($"{msg.say}, {msg.to}"); // your console may not support Unicode
        }
        
        [Test]
        public void HelloWorldWriter() {
            var r = new JsonWriter(new PropType.Store());
            r.Write(new Message {say = "Hello 👋", to = "World"});
            Console.WriteLine(r.Output.ToString()); // your console may not support Unicode
        }

    }
#endif
}