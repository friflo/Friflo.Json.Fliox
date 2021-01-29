using System.Linq;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    class ChildIL
    {
        public int val;
    }
    
    struct StructIL
    {
        public int val2;
    }
    
    class SampleIL {
        // public StructIL childStruct1;
        // public StructIL childStruct2;
        public ChildIL  child;
        public ChildIL  childNull;
        public double   dbl;
        public float    flt;
              
        public long     int64;
        public int      int32;
        public short    int16;
        public byte     int8;
              
        public bool     bln;

        public void Init() {
            child = new ChildIL { val = 42 };
            // childStruct1.val2 = 111;
            // childStruct2.val2 = 112;
            childNull = null;
            dbl   = 22.5d;
            flt   = 33.5f;
            
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
    ""child"": {{
        ""val"": 42
    }},
    ""childNull"": null,
    ""dbl"":   22.5,
    ""flt"":   33.5,

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
    ""child"": {{
        ""val"": 42
    }},
    ""childNull"": null,
    ""dbl"":   22.5,
    ""flt"":   33.5,

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
                
                AreEqual(22.5,  result.dbl);
                AreEqual(33.5,  result.flt);
                
                AreEqual(10,    result.int64);
                AreEqual(11,    result.int32);
                AreEqual(12,    result.int16);
                AreEqual(13,    result.int8);
                AreEqual(true,  result.bln);
                AreEqual(42,    result.child.val);
                AreEqual(null,  result.childNull);
            }
        }  
    }
    
    public class TestILPerformance {
        public Child   child1;
        public Child   child2;
        public Child   child3;
        public Child   child4;
        public Child   child5;
        public Child   child6;
        public Child   child7;
        public Child   child8;

        public class Child {
        }
        
        [Test]
        public void RunSetValue () {
            string payloadStr = $@"
{{
    ""child1"":   {{}},
    ""child2"":   {{}},
    ""child3"":   {{}},
    ""child4"":   {{}},
    ""child5"":   {{}},
    ""child6"":   {{}},
    ""child7"":   {{}},
    ""child8"":   {{}},
}}
";
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (Bytes        json        = new Bytes(payloadStr)) {
                var obj = new TestILPerformance();
                for (int n = 0; n < 5; n++)
                    writer.Write(obj);
            }
        }
    }
}

#endif

