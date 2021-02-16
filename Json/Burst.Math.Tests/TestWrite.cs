using System.Linq;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Burst.Math.Tests
{
    public class TestWrite
    {
        [Test]
        public void WriteMath() {
            var s = new JsonSerializer();
            MathKeys    keys = new MathKeys(Default.Constructor);
            s.InitSerializer();
            try {
                var types = new MathTypes();
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
            
            s.MemberArrayStart(k.float2);
            JsonMath.Write(ref s, types.float2);
            s.ArrayEnd();
            
            s.MemberArrayStart(k.float3);
            JsonMath.Write(ref s, types.float3);
            s.ArrayEnd();
            
            s.MemberArrayStart(k.float4);
            JsonMath.Write(ref s, types.float4);
            s.ArrayEnd();
            
            s.ObjectEnd();
        }
    }
}