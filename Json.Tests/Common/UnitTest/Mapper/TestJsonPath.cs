using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Graph;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestJsonPath : LeakTestsFixture
    {
        // [Test]
        public void TestSelect() {
            using (var typeStore    = new TypeStore()) 
            using (var jsonWriter   = new JsonWriter(typeStore))
            using (var jsonPath     = new JsonPath())
            {
                var sample = new SampleIL();
                var json = jsonWriter.Write(sample);

                jsonPath.Select(json, "/childStructNull1");
            }
        }
    }
}