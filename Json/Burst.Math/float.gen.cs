// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math
{
    public static partial class Json
    {
        public static bool UseMemberFloatX2(this ref JObj i, ref JsonParser p, in Str32 key, ref float2 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat2(ref JArr i, ref JsonParser p, ref float2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 2)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberFloatX3(this ref JObj i, ref JsonParser p, in Str32 key, ref float3 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat3(ref JArr i, ref JsonParser p, ref float3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 3)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberFloatX4(this ref JObj i, ref JsonParser p, in Str32 key, ref float4 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat4(ref JArr i, ref JsonParser p, ref float4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 4)
                        value[index++] = p.ValueAsFloat(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        private static void ReadFloat2x2(ref JArr i, ref JsonParser p, ref float2x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadFloat2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX2x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref float2x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat2x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat2x3(ref JArr i, ref JsonParser p, ref float2x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadFloat2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX2x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref float2x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat2x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat2x4(ref JArr i, ref JsonParser p, ref float2x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadFloat2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX2x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref float2x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat2x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat3x2(ref JArr i, ref JsonParser p, ref float3x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadFloat3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX3x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref float3x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat3x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat3x3(ref JArr i, ref JsonParser p, ref float3x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadFloat3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX3x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref float3x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat3x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat3x4(ref JArr i, ref JsonParser p, ref float3x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadFloat3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX3x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref float3x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat3x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat4x2(ref JArr i, ref JsonParser p, ref float4x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadFloat4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX4x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref float4x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat4x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat4x3(ref JArr i, ref JsonParser p, ref float4x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadFloat4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX4x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref float4x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat4x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadFloat4x4(ref JArr i, ref JsonParser p, ref float4x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadFloat4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberFloatX4x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref float4x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadFloat4x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }
    }
}
