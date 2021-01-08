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
                serializer.ArrayEnd();
                AreEqual("[[],[],{}]", serializer.dst.ToString());
            }
#if DEBUG
            // test DEBUG safety guards
            Throws<InvalidOperationException>(()=> {
                ser.InitSerializer();
                ser.ObjectStart();
                ser.ObjectStart();
            });
            Throws<InvalidOperationException>(()=> {
                ser.InitSerializer();
                ser.ObjectStart();
                ser.ArrayStart();
            });
#endif
        }
    }
}