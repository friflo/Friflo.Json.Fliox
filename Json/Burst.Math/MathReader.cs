using Unity.Mathematics;

namespace Friflo.Json.Burst.Math
{
    public static class MathReader
    {
        public static void Read(ref JsonParser p, ref float2 value) {
            int index = 0;
            var i = p.GetArrayIterator();
            while (p.NextArrayElement(ref i)) {
                if (p.UseElementNum(ref i)) {
                    if (index < 2)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static void Read(ref JsonParser p, ref float3 value) {
            int index = 0;
            var i = p.GetArrayIterator();
            while (p.NextArrayElement(ref i)) {
                if (p.UseElementNum(ref i)) {
                    if (index < 3)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static void Read(ref JsonParser p, ref float4 value) {
            int index = 0;
            var i = p.GetArrayIterator();
            while (p.NextArrayElement(ref i)) {
                if (p.UseElementNum(ref i)) {
                    if (index < 4)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
    }
}