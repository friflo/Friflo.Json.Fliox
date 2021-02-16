using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst.Math
{
    public static partial class JsonMath
    {
        public static void MemberFloat2(this ref JsonSerializer s, in Str32 key, in float2 value) {
            s.MemberArrayStart(key);
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ArrayEnd();
        }
        
        public static void MemberFloat3(this ref JsonSerializer s, in Str32 key, in float3 value) {
            s.MemberArrayStart(key);
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ArrayEnd();
        }
        
        public static void MemberFloat4(this ref JsonSerializer s, in Str32 key, in float4 value) {
            s.MemberArrayStart(key);
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
            s.ArrayEnd();
        }
        
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