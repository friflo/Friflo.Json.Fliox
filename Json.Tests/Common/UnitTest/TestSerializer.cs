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

        private void RunSerializer(ref JsonSerializer serializer) {
            JsonSerializer ser = serializer; // capture
            {
                serializer.InitSerializer();
                serializer.ObjectStart();
                serializer.ObjectEnd();
                AreEqual("{}", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ArrayStart();
                serializer.ArrayEnd();
                AreEqual("[]", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ArrayStart();
                    serializer.ArrayStart();
                    serializer.ArrayEnd();
                    serializer.ArrayStart();
                    serializer.ArrayEnd();
                    serializer.ObjectStart();
                    serializer.ObjectEnd();
                    serializer.ElementString("hello");
                    serializer.ElementDouble(10.5);
                    serializer.ElementLong(42);
                    serializer.ElementBool(true);
                    serializer.ElementNull();
                serializer.ArrayEnd();
                AreEqual("[[],[],{},\"hello\",10.5,42,true,null]", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ObjectStart();
                    serializer.MemberString("string", "World");
                    serializer.MemberDouble("double", 10.5);
                    serializer.MemberLong("long", 42);
                    serializer.MemberBool("bool", true);
                    serializer.MemberNull("null");
                serializer.ObjectEnd();
                AreEqual("{\"string\":\"World\",\"double\":10.5,\"long\":42,\"bool\":true,\"null\":null}", serializer.dst.ToString());
            }
            // --- Primitives on root level ---
            {
                serializer.InitSerializer();
                serializer.ElementString("hello");
                AreEqual("\"hello\"", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ElementLong(42);
                AreEqual("42", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ElementDouble(10.5);
                AreEqual("10.5", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ElementBool(true);
                AreEqual("true", serializer.dst.ToString());
            } {
                serializer.InitSerializer();
                serializer.ElementNull();
                AreEqual("null", serializer.dst.ToString());
            }
#if DEBUG
            // test DEBUG safety guards
            Throws<InvalidOperationException>(()=> {
                ser.InitSerializer();
                ser.ObjectStart();
                ser.ObjectStart(); // object can only start in an object via MemberObjectStart();
            });
            Throws<InvalidOperationException>(()=> {
                ser.InitSerializer();
                ser.ObjectStart();
                ser.ArrayStart(); // array can only start in an object via MemberArrayStart();
            });
            Throws<InvalidOperationException>(()=> {
                ser.InitSerializer();
                ser.MemberBool("Test", true); // member not in object
            });
#endif
        }
    }
}