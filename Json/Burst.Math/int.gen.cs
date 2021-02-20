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
        public static bool UseMemberInt2(this ref JObj i, ref JsonParser p, in Str32 key, ref int2 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt2(ref JArr i, ref JsonParser p, ref int2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 2)
                        value[index++] = p.ValueAsInt(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberInt3(this ref JObj i, ref JsonParser p, in Str32 key, ref int3 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt3(ref JArr i, ref JsonParser p, ref int3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 3)
                        value[index++] = p.ValueAsInt(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberInt4(this ref JObj i, ref JsonParser p, in Str32 key, ref int4 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt4(ref JArr i, ref JsonParser p, ref int4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 4)
                        value[index++] = p.ValueAsInt(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        private static void ReadInt2x2(ref JArr i, ref JsonParser p, ref int2x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadInt2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt2x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref int2x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt2x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt2x3(ref JArr i, ref JsonParser p, ref int2x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadInt2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt2x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref int2x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt2x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt2x4(ref JArr i, ref JsonParser p, ref int2x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadInt2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt2x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref int2x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt2x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt3x2(ref JArr i, ref JsonParser p, ref int3x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadInt3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt3x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref int3x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt3x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt3x3(ref JArr i, ref JsonParser p, ref int3x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadInt3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt3x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref int3x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt3x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt3x4(ref JArr i, ref JsonParser p, ref int3x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadInt3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt3x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref int3x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt3x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt4x2(ref JArr i, ref JsonParser p, ref int4x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadInt4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt4x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref int4x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt4x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt4x3(ref JArr i, ref JsonParser p, ref int4x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadInt4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt4x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref int4x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt4x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadInt4x4(ref JArr i, ref JsonParser p, ref int4x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadInt4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberInt4x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref int4x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadInt4x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }
    }
}
