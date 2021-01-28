using System.Linq;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    class SampleIL {
        public long     int64;
        public int      int32;
        public short    int16;
        public byte     int8;
        
        public bool     bln;

        public void Init() {
            int64 = 10;
            int32 = 11;
            int16 = 12;
            int8  = 13;
            bln   = true;
        }
    }
    
    public class TestILClassMapper
    {
        [Test]
        public void WriteJson() {

            string payloadStr = $@"
{{
    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13,
    ""bln"":   true
}}
";
            string payloadTrimmed = string.Concat(payloadStr.Where(c => !char.IsWhiteSpace(c)));
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var sample = new SampleIL();
                sample.Init();
                writer.Write(sample);
                AreEqual(payloadTrimmed, writer.Output.ToString());
            }
        }
        
        [Test]
        public void ReadJson() {

            string payloadStr = $@"
{{
    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13,
    ""bln"":   true
}}
";
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (Bytes        json        = new Bytes(payloadStr))
            {
                var result = reader.Read<SampleIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                
                AreEqual(10,    result.int64);
                AreEqual(11,    result.int32);
                AreEqual(12,    result.int16);
                AreEqual(13,    result.int8);
                AreEqual(true,  result.bln);
            }
        }  
    }
}

#endif
