// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
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
        public static bool UseMemberFloat2(this ref JsonParser p, ref ObjectIterator iterator, in Str32 key, ref float2 value) {
            if (p.UseMemberArr(ref iterator, in key)) {
                ReadFloat2(ref p, ref value);
                return true;
            }
            return false;
        }
        
        public static bool UseMemberFloat3(this ref JsonParser p, ref ObjectIterator iterator, in Str32 key, ref float3 value) {
            if (p.UseMemberArr(ref iterator, in key)) {
                ReadFloat3(ref p, ref value);
                return true;
            }
            return false;
        }
        
        public static bool UseMemberFloat4(this ref JsonParser p, ref ObjectIterator iterator, in Str32 key, ref float4 value) {
            if (p.UseMemberArr(ref iterator, in key)) {
                ReadFloat4(ref p, ref value);
                return true;
            }
            return false;
        }

        
        public static void ReadFloat2(ref JsonParser p, ref float2 value) {
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
        
        public static void ReadFloat3(ref JsonParser p, ref float3 value) {
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
        
        public static void ReadFloat4(ref JsonParser p, ref float4 value) {
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