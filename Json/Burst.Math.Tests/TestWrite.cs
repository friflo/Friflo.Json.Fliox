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
            
            s.InitSerializer();
            try {
                types.InitSample();
                WriteMathTypes(ref s, in keys, types);

                var expect = @"
{
    ""float2"": [1.0,2.0],
    ""float3"": [1.0,2.0,3.0],
    ""float4"": [1.0,2.0,3.0,4.0]
}";
                string expectTrimmed = string.Concat(expect.Where(c => !char.IsWhiteSpace(c)));
                AreEqual(expectTrimmed.Trim(), s.json.ToString());
            }
            finally {
                s.Dispose();
            }

        }
        
        private static void WriteMathTypes(ref JsonSerializer s, in MathKeys k, in MathTypes types) {
            s.ObjectStart();
            
            s.MemberFloat2(k.float2, types.float2);
            s.MemberFloat3(k.float3, types.float3);
            s.MemberFloat4(k.float4, types.float4);
            
            s.ObjectEnd();
        }
    }
}