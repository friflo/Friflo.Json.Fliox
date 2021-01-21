using System; // for DEBUG
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;
#pragma warning disable 618

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestSerializer
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
                AreEqual("{}", s.dst.ToString());
                AreEqual(0, s.Level);
            } {
                s.InitSerializer();
                s.ArrayStart();
                s.ArrayEnd();
                AreEqual("[]", s.dst.ToString());
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
                AreEqual("[[],{},\"hello\",10.5,42,true,null]", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberStrVal("string", "World");
                    s.MemberDblVal("double", 10.5);
                    s.MemberDblVal("long", 42);
                    s.MemberBlnVal("bool", true);
                    s.MemberNulVal("null");
                s.ObjectEnd();
                AreEqual("{\"string\":\"World\",\"double\":10.5,\"long\":42,\"bool\":true,\"null\":null}", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberArrayStartVal("array");
                    s.ArrayEnd();
                    
                    s.MemberObjectStartVal("object");
                    s.ObjectEnd();
                s.ObjectEnd();
                AreEqual("{\"array\":[],\"object\":{}}", s.dst.ToString());
            }
            // --- ensure coverage of methods using Bytes as parameter
            Bytes textValue = new Bytes("textValue");
            Bytes str = new Bytes("str");
            Bytes dbl = new Bytes("dbl");
            Bytes lng = new Bytes("lng");
            try {
                // - array
                s.InitSerializer();
                s.ArrayStart();
                s.ElementStr(ref textValue);
                s.ArrayEnd();
                AreEqual("[\"textValue\"]", s.dst.ToString());
                
                // - object
                s.InitSerializer();
                s.ObjectStart();
                s.MemberStr(ref str, "hello");
                s.MemberDbl(ref dbl, 10.5);
                s.MemberDbl(ref lng, 42);
                s.ObjectEnd();
                AreEqual("{\"str\":\"hello\",\"dbl\":10.5,\"lng\":42}", s.dst.ToString());
            }
            finally {
                // only required for Unity/JSON_BURST
                lng.Dispose();
                str.Dispose();
                dbl.Dispose();
                textValue.Dispose();
            }

            // --- Primitives on root level ---
            {
                s.InitSerializer();
                s.ElementStr("hello");
                AreEqual("\"hello\"", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementLng(42);
                AreEqual("42", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementDbl(10.5);
                AreEqual("10.5", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementBln(true);
                AreEqual("true", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementNul();
                AreEqual("null", s.dst.ToString());
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
                    ser.MemberBlnVal("Test", true); // member not in root
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ArrayStart();
                    ser.MemberBlnVal("Test", true); // member not in array
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
    }
}