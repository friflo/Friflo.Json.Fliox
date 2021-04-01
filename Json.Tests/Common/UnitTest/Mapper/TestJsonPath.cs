using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Graph;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestJsonPath : LeakTestsFixture
    {
        [Test]
        public void TestSelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new JsonWriter(typeStore))
            using (var jsonPath     = new JsonPath())
            {
                var sample = new SampleIL();
                var json = jsonWriter.Write(sample);

                var result = jsonPath.Select(json, new [] {
                    ".childStructNull1",
                    ".childStructNull2.val2",
                    ".dbl",
                    ".bln",
                    ".enumIL1",
                    ".child"
                });
                AreEqual(@"{""val2"":68}",  result[0]);
                AreEqual("69",              result[1]);
                AreEqual("94.0",            result[2]);
                AreEqual("true",            result[3]);
                AreEqual(@"""one""",        result[4]);
                AreEqual("null",            result[5]);
                
            }
        }
    }
}