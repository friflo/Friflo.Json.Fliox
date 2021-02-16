using NUnit.Framework;
using Unity.Mathematics;
using static NUnit.Framework.Assert;



namespace Friflo.Json.Burst.Math.Tests
{
    public class TestRead
    {
        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""float2"":  [1,2],
    ""float3"":  [1,2,3],
    ""float4"":  [1,2,3,4]
}";

        [Test]
        public void ReadMath() {
            var   types = new MathTypes();
            MathKeys    k = new MathKeys(Default.Constructor);
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString);
            try {
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                ReadMathTypes(ref p, ref k, ref types);

                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                
                AreEqual(JsonEvent.EOF, p.NextEvent()); // Important to ensure absence of application errors
                
                AreEqual(new float2(1,2),       types.float2);
                AreEqual(new float3(1,2,3),     types.float3);
                AreEqual(new float4(1,2,3,4),   types.float4);
            }
            finally {
                // only required for Unity/JSON_BURST
                json.Dispose();
                p.Dispose();
            }
        }
        
        private static void ReadMathTypes(ref JsonParser p, ref MathKeys k, ref MathTypes types) {
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if      (p.UseMemberArr (ref i, in k.float2))      { JsonMath.Read(ref p, ref types.float2); }
                else if (p.UseMemberArr (ref i, in k.float3))      { JsonMath.Read(ref p, ref types.float3); }
                else if (p.UseMemberArr (ref i, in k.float4))      { JsonMath.Read(ref p, ref types.float4); }
            }
        }

    }
}