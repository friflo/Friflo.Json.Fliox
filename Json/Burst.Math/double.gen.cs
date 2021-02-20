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
        public static bool UseMemberDoubleX2(this ref JObj i, ref JsonParser p, in Str32 key, ref double2 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble2(ref JArr i, ref JsonParser p, ref double2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 2)
                        value[index++] = p.ValueAsDouble(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberDoubleX3(this ref JObj i, ref JsonParser p, in Str32 key, ref double3 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble3(ref JArr i, ref JsonParser p, ref double3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 3)
                        value[index++] = p.ValueAsDouble(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberDoubleX4(this ref JObj i, ref JsonParser p, in Str32 key, ref double4 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble4(ref JArr i, ref JsonParser p, ref double4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p)) {
                    if (index < 4)
                        value[index++] = p.ValueAsDouble(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        private static void ReadDouble2x2(ref JArr i, ref JsonParser p, ref double2x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadDouble2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX2x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref double2x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble2x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble2x3(ref JArr i, ref JsonParser p, ref double2x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadDouble2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX2x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref double2x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble2x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble2x4(ref JArr i, ref JsonParser p, ref double2x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadDouble2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX2x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref double2x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble2x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble3x2(ref JArr i, ref JsonParser p, ref double3x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadDouble3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX3x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref double3x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble3x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble3x3(ref JArr i, ref JsonParser p, ref double3x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadDouble3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX3x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref double3x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble3x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble3x4(ref JArr i, ref JsonParser p, ref double3x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadDouble3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX3x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref double3x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble3x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble4x2(ref JArr i, ref JsonParser p, ref double4x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadDouble4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX4x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref double4x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble4x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble4x3(ref JArr i, ref JsonParser p, ref double4x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadDouble4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX4x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref double4x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble4x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadDouble4x4(ref JArr i, ref JsonParser p, ref double4x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadDouble4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }
        
        public static bool UseMemberDoubleX4x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref double4x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadDouble4x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }
    }
}
