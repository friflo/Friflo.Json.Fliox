using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst.Math.Tests
{
    public struct MathTypes {
        public  float2  float2;
        public  float3  float3;
        public  float4  float4;
    }

    // Using a struct containing JSON key names enables using them by ref to avoid memcpy
    public struct MathKeys {
        public Str32    float2;
        public Str32    float3;
        public Str32    float4;

        public MathKeys(Default _) {
            float2 = "float2";
            float3 = "float3";
            float4 = "float4";
        }
    }
}