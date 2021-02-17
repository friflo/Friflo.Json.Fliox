using System.Linq;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Burst.Math.Tests
{
    public class TestWrite
    {
        [Test]
        public void WriteMath() {
            var types   = new MathTypes();
            var s       = new JsonSerializer();
            var keys    = new MathKeys(Default.Constructor);
            
            s.SetPretty(true);
            s.InitSerializer();
            try {
                types.InitSample();
                WriteMathTypes(ref s, in keys, types);

                var expect = @"{
    ""float2"": [1.0, 2.0],
    ""float3"": [1.0, 2.0, 3.0],
    ""float4"": [1.0, 2.0, 3.0, 4.0],
    ""float4x4"": [
        [1.0, 2.0, 3.0, 4.0],
        [11.0, 12.0, 13.0, 14.0],
        [21.0, 22.0, 23.0, 24.0],
        [31.0, 32.0, 33.0, 34.0]
    ]
}";
                expect = expect.Replace("\r\n", "\n"); // CR LF -> LF
                AreEqual(expect.Trim(), s.json.ToString());
            }
            finally {
                s.Dispose();
            }

        }
        
        private static void WriteMathTypes(ref JsonSerializer s, in MathKeys k, in MathTypes types) {
            s.ObjectStart();
            
            s.MemberFloat2  (k.float2,    types.float2);
            s.MemberFloat3  (k.float3,    types.float3);
            s.MemberFloat4  (k.float4,    types.float4);
            s.MemberFloat4x4(k.float4x4,  types.float4x4);
            
            s.ObjectEnd();
        }
    }
}