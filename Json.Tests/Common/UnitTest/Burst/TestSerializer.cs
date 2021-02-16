using System; // for DEBUG
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestSerializer : LeakTestsFixture
    {
        [Test]
        public void TestBasics() {
            JsonSerializer serializer = new JsonSerializer();
            try {
                RunSerializer(ref serializer);
            }
            finally {
                serializer.Dispose();
            }
        }

        private void RunSerializer(ref JsonSerializer s) {
            {
                s.InitSerializer();
                s.ObjectStart();
                s.ObjectEnd();
                AreEqual("{}", s.json.ToString());
                AreEqual(0, s.Level);
            } {
                s.InitSerializer();
                s.ArrayStart();
                s.ArrayEnd();
                AreEqual("[]", s.json.ToString());
            } {
                s.InitSerializer();
                s.ArrayStart();
                    s.ArrayStart();
                    s.ArrayEnd();
                    s.ObjectStart();
                    s.ObjectEnd();
                    s.ElementStr("hello");
                    s.ElementDbl(10.5);
                    s.ElementLng(42);
                    s.ElementBln(true);
                    s.ElementNul();
                s.ArrayEnd();
                AreEqual("[[],{},\"hello\",10.5,42,true,null]", s.json.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberStr ("string 👋", "World 🌎");
                    s.MemberDbl ("double 👋", 10.5);
                    s.MemberLng ("long 👋", 42);
                    s.MemberBln ("bool 👋", true);
                    s.MemberNul ("null 👋");
                s.ObjectEnd();
                AreEqual("{\"string 👋\":\"World 🌎\",\"double 👋\":10.5,\"long 👋\":42,\"bool 👋\":true,\"null 👋\":null}", s.json.ToString());
            } {
                // Escape string (or FixedString32 when compiling with JSON_BURST)
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr ("1-\r", "World 🌎");
                s.MemberDbl ("2-\n", 10.5);
                s.MemberLng ("3-\t", 42);
                s.MemberBln ("4-\"", true);
                s.MemberNul ("5-\\");
                s.MemberNul ("6-\b");
                s.MemberNul ("7-\f");
                s.ObjectEnd();
                AreEqual("{\"1-\\r\":\"World 🌎\",\"2-\\n\":10.5,\"3-\\t\":42,\"4-\\\"\":true,\"5-\\\\\":null,\"6-\\b\":null,\"7-\\f\":null}", s.json.ToString());
            } {
                using (var key1 = new Bytes("1-\r"))
                using (var key2 = new Bytes("2-\n"))
                using (var key3 = new Bytes("3-\t"))
                using (var key4 = new Bytes("4-\""))
                using (var key5 = new Bytes("5-\\"))
                using (var key6 = new Bytes("6-\b"))
                using (var key7 = new Bytes("7-\f"))
                {
                    s.InitSerializer();
                    s.ObjectStart();
                    s.MemberStr(in key1, "World 🌎");
                    s.MemberDbl(in key2, 10.5);
                    s.MemberLng(in key3, 42);
                    s.MemberBln(in key4, true);
                    s.MemberNul(in key5);
                    s.MemberNul(in key6);
                    s.MemberNul(in key7);
                    s.ObjectEnd();
                }
                AreEqual("{\"1-\\r\":\"World 🌎\",\"2-\\n\":10.5,\"3-\\t\":42,\"4-\\\"\":true,\"5-\\\\\":null,\"6-\\b\":null,\"7-\\f\":null}", s.json.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberArrayStart ("array");
                    s.ArrayEnd();
                    
                    s.MemberObjectStart ("object");
                    s.ObjectEnd();
                s.ObjectEnd();
                AreEqual("{\"array\":[],\"object\":{}}", s.json.ToString());
            }
            // --- ensure coverage of methods using Bytes as parameter
            using (var textValue = new Bytes("textValue"))
            using (var str = new Bytes("str"))
            using (var dbl = new Bytes("dbl"))
            using (var lng = new Bytes("lng"))
            {
                // - array
                s.InitSerializer();
                s.ArrayStart();
                s.ElementStr(in textValue);
                s.ArrayEnd();
                AreEqual("[\"textValue\"]", s.json.ToString());
                
                // - object
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr(in str, "hello");
                s.MemberDbl(in dbl, 10.5);
                s.MemberLng(in lng, 42);
                s.ObjectEnd();
                AreEqual("{\"str\":\"hello\",\"dbl\":10.5,\"lng\":42}", s.json.ToString());
            }

            // --- Primitives on root level ---
            {
                s.InitSerializer();
                s.ElementStr("hello");
                AreEqual("\"hello\"", s.json.ToString());
            } {
                s.InitSerializer();
                s.ElementLng(42);
                AreEqual("42", s.json.ToString());
            } {
                s.InitSerializer();
                s.ElementDbl(10.5);
                AreEqual("10.5", s.json.ToString());
            } {
                s.InitSerializer();
                s.ElementBln(true);
                AreEqual("true", s.json.ToString());
            } {
                s.InitSerializer();
                s.ElementNul();
                AreEqual("null", s.json.ToString());
            }
#if DEBUG
            JsonSerializer ser = s; // capture
            
            // --- test DEBUG safety guard exceptions ---
            {
                var e = Throws<InvalidOperationException>(() =>
                {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ObjectStart(); // object can only start in an object via MemberObjectStart();
                });
                AreEqual("ObjectStart() and ArrayStart() requires a previous call to a ...Start() method", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ArrayStart(); // array can only start in an object via MemberArrayStart();
                });
                AreEqual("ObjectStart() and ArrayStart() requires a previous call to a ...Start() method", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.MemberBln ("Test", true); // member not in root
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ArrayStart();
                    ser.MemberBln ("Test", true); // member not in array
                });
                AreEqual("Member...() methods and ObjectEnd() must be called only within an object", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ElementBln(true); // element not in object
                });
                AreEqual("Element...() methods and ArrayEnd() must be called only within an array or on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ArrayEnd();
                });
                AreEqual("Element...() methods and ArrayEnd() must be called only within an array or on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ArrayStart();
                    ser.ObjectEnd();
                });
                AreEqual("Member...() methods and ObjectEnd() must be called only within an object", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ObjectEnd();
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(() => {
                    ser.InitSerializer();
                    ser.ArrayEnd();
                });
                AreEqual("ArrayEnd...() must not be called below root level", e.Message);
            }
#endif
        }
        
        [Test]
        public void TestMaxDepth() {
            using (JsonSerializer ser = new JsonSerializer())
            {
                // --- JsonSerializer
                ser.InitSerializer();
                ser.SetMaxDepth(1);
                
                // case OK
                ser.ArrayStart();
                ser.ArrayEnd();
                AreEqual(0, ser.Level);
                AreEqual("[]", ser.json.ToString());
                
                // case exception
                ser.ArrayStart();
                var e = Throws<InvalidOperationException>(() => ser.ArrayStart()); // add second array
                AreEqual("JsonSerializer exceed maxDepth: 1", e.Message);
            }
        }
        
        [Test]
        public void Pretty () {
            var parser = new JsonParser();
            try {
                using (var ser = new JsonSerializer())
                using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                    ser.SetPretty(true);
                    parser.InitParser(bytes);
                    ser.InitSerializer();
                    ser.WriteTree(ref parser);
                    CommonUtils.ToFile("assets/output/complexPrettySerializer.json", ser.json);
                }
            } finally {
                parser.Dispose();
            }
        }
    }
}