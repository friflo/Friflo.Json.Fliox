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
            }
            {
                serializer.InitSerializer();
                serializer.ArrayStart();
                    serializer.ArrayStart();
                    serializer.ArrayEnd();
                    serializer.ArrayStart();
                    serializer.ArrayEnd();
                serializer.ArrayEnd();
                AreEqual("[[],[]]", serializer.dst.ToString());
            }
        }
    }
}