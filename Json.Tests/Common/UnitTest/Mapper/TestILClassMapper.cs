using System.Linq;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    class SampleIL {
        public long     int64 = 10;
        public int      int32 = 11;
        public short    int16 = 12;
        public byte     int8  = 13;
    }
    
    public class TestILClassMapper
    {
        [Test]
        public void Run() {

            string payloadStr = $@"
{{
    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13
}}
";
            string payloadTrimmed = string.Concat(payloadStr.Where(c => !char.IsWhiteSpace(c)));
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore typeStore = new TypeStore(resolver))
            using (JsonWriter writer = new JsonWriter(typeStore))
            {
                var sample = new SampleIL();
                writer.Write(sample);
                AreEqual(payloadTrimmed, writer.Output.ToString());
            }
        }  
    }
}