using Friflo.Json.Burst;
using Friflo.Json.Burst.Math;
using NUnit.Framework;
using Unity.Mathematics;
using static NUnit.Framework.Assert;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str32 = System.String;
    using Str128 = System.String;
#endif

namespace Friflo.Json.Tests.Math
{
    public class TestMath
    {

        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""float2"":  [1,2],
    ""float3"":  [1,2,3],
    ""float4"":  [1,2,3,4]
}";
        public struct MathTypes {
            public  float2  float2;
            public  float3  float3;
            public  float4  float4;
        }

        // Using a struct containing JSON key names enables using them by ref to avoid memcpy
        public struct Keys {
            public Str32    float2;
            public Str32    float3;
            public Str32    float4;

            public Keys(Default _) {
                float2 = "float2";
                float3 = "float3";
                float4 = "float4";
            }
        }

        [Test]
        public void ReadMath() {
            var   types = new MathTypes();
            Keys    k = new Keys(Default.Constructor);
            
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
        
        private static void ReadMathTypes(ref JsonParser p, ref Keys k, ref MathTypes types) {
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if      (p.UseMemberArr (ref i, in k.float2))      { MathReader.Read(ref p, ref types.float2); }
                else if (p.UseMemberArr (ref i, in k.float3))      { MathReader.Read(ref p, ref types.float3); }
                else if (p.UseMemberArr (ref i, in k.float4))      { MathReader.Read(ref p, ref types.float4); }
            }
        }

    }
}