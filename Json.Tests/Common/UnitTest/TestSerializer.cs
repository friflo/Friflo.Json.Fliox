using System; // for DEBUG
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest
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
                    s.ArrayStart();
                    s.ArrayEnd();
                    s.ObjectStart();
                    s.ObjectEnd();
                    s.ElementString("hello");
                    s.ElementDouble(10.5);
                    s.ElementLong(42);
                    s.ElementBool(true);
                    s.ElementNull();
                s.ArrayEnd();
                AreEqual("[[],[],{},\"hello\",10.5,42,true,null]", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberString("string", "World");
                    s.MemberDouble("double", 10.5);
                    s.MemberLong("long", 42);
                    s.MemberBool("bool", true);
                    s.MemberNull("null");
                s.ObjectEnd();
                AreEqual("{\"string\":\"World\",\"double\":10.5,\"long\":42,\"bool\":true,\"null\":null}", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ObjectStart();
                    s.MemberArrayStart("array");
                    s.ArrayEnd();
                    
                    s.MemberObjectStart("object");
                    s.ObjectEnd();
                s.ObjectEnd();
                AreEqual("{\"array\":[],\"object\":{}}", s.dst.ToString());
            }
            // --- Primitives on root level ---
            {
                s.InitSerializer();
                s.ElementString("hello");
                AreEqual("\"hello\"", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementLong(42);
                AreEqual("42", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementDouble(10.5);
                AreEqual("10.5", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementBool(true);
                AreEqual("true", s.dst.ToString());
            } {
                s.InitSerializer();
                s.ElementNull();
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
                    ser.MemberBool("Test", true); // member not in root
                });
                AreEqual("Member...() methods and ObjectEnd() must not be called on root level", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ArrayStart();
                    ser.MemberBool("Test", true); // member not in array
                });
                AreEqual("Member...() methods and ObjectEnd() must be called only within an object", e.Message);
            } {
                var e = Throws<InvalidOperationException>(()=> {
                    ser.InitSerializer();
                    ser.ObjectStart();
                    ser.ElementBool(true); // element not in object
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