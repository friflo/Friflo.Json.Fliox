using Unity.Mathematics;

namespace Friflo.Json.Burst.Math
{
    public static partial class JsonMath
    {
        public static void Write(ref JsonSerializer s, in float2 value) {
            s.ArrayStart();
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ArrayEnd();
        }
        
        public static void Write(ref JsonSerializer s, in float3 value) {
            s.ArrayStart();
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ArrayEnd();
        }
        
        public static void Write(ref JsonSerializer s, in float4 value) {
            s.ArrayStart();
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
            s.ArrayEnd();
        }
    }
}