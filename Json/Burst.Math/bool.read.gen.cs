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
        public static bool UseMemberBool2(this ref JObj i, ref JsonParser p, in Str32 key, ref bool2 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool2(ref JArr i, ref JsonParser p, ref bool2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementBln(ref p)) {
                    if (index < 2)
                        value[index++] = p.ValueAsBool(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool3(this ref JObj i, ref JsonParser p, in Str32 key, ref bool3 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool3(ref JArr i, ref JsonParser p, ref bool3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementBln(ref p)) {
                    if (index < 3)
                        value[index++] = p.ValueAsBool(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool4(this ref JObj i, ref JsonParser p, in Str32 key, ref bool4 value) {
            if (i.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool4(ref JArr i, ref JsonParser p, ref bool4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementBln(ref p)) {
                    if (index < 4)
                        value[index++] = p.ValueAsBool(out bool _);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        private static void ReadBool2x2(ref JArr i, ref JsonParser p, ref bool2x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadBool2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool2x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool2x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool2x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool2x3(ref JArr i, ref JsonParser p, ref bool2x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadBool2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool2x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool2x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool2x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool2x4(ref JArr i, ref JsonParser p, ref bool2x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadBool2(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool2x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool2x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool2x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool3x2(ref JArr i, ref JsonParser p, ref bool3x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadBool3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool3x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool3x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool3x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool3x3(ref JArr i, ref JsonParser p, ref bool3x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadBool3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool3x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool3x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool3x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool3x4(ref JArr i, ref JsonParser p, ref bool3x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadBool3(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool3x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool3x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool3x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool4x2(ref JArr i, ref JsonParser p, ref bool4x2 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 2)
                        ReadBool4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool4x2(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool4x2 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool4x2(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool4x3(ref JArr i, ref JsonParser p, ref bool4x3 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 3)
                        ReadBool4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool4x3(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool4x3 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool4x3(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }

        private static void ReadBool4x4(ref JArr i, ref JsonParser p, ref bool4x4 value) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementArr(ref p, out JArr arr)) {
                    if (index < 4)
                        ReadBool4(ref arr, ref p, ref value[index++]);
                } else 
                    p.ErrorMsg("Json.Burst.Math", "expect JSON number");
            }
        }

        public static bool UseMemberBool4x4(this ref JObj obj, ref JsonParser p, in Str32 key, ref bool4x4 value) {
            if (obj.UseMemberArr(ref p, in key, out JArr arr)) {
                ReadBool4x4(ref arr, ref p, ref value);
                return true;
            }
            return false;
        }
    }
}
