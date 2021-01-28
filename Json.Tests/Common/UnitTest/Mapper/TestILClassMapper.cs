using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    class SampleIL {
        public int key = 11;
    }
    
    public class TestILClassMapper
    {
        [Test]
        public void Run() {

            string payloadStr = $@"
{{
    ""key"": 11
}}
";
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore typeStore = new TypeStore(resolver))
            using (JsonWriter writer = new JsonWriter(typeStore))
            {
                var sample = new SampleIL();
                writer.Write(sample);
                AreEqual("{\"key\":11}", writer.Output.ToString());
            }
        }  
    }
}