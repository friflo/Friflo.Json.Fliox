using Unity.Mathematics;

namespace Friflo.Json.Burst.Math
{
    public static partial class JsonMath
    {
        public static void Write(ref JsonSerializer s, in float2 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
        }
        
        public static void Write(ref JsonSerializer s, in float3 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
        }
        
        public static void Write(ref JsonSerializer s, in float4 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
        }
    }
}