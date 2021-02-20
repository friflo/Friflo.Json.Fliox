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
        public static void MemberBool2(this ref JsonSerializer s, in Str32 key, in bool2 value) {
            s.MemberArrayStart(key, false);
            WriteBool2(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayBool2(this ref JsonSerializer s, in bool2 value) {
            s.ArrayStart(false);
            WriteBool2(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteBool2(ref JsonSerializer s, in bool2 value) {
            s.ElementBln(value.x);
            s.ElementBln(value.y);
        }

        public static void MemberBool3(this ref JsonSerializer s, in Str32 key, in bool3 value) {
            s.MemberArrayStart(key, false);
            WriteBool3(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayBool3(this ref JsonSerializer s, in bool3 value) {
            s.ArrayStart(false);
            WriteBool3(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteBool3(ref JsonSerializer s, in bool3 value) {
            s.ElementBln(value.x);
            s.ElementBln(value.y);
            s.ElementBln(value.z);
        }

        public static void MemberBool4(this ref JsonSerializer s, in Str32 key, in bool4 value) {
            s.MemberArrayStart(key, false);
            WriteBool4(ref s, in value);
            s.ArrayEnd();
        }

        public static void ArrayBool4(this ref JsonSerializer s, in bool4 value) {
            s.ArrayStart(false);
            WriteBool4(ref s, in value);
            s.ArrayEnd();
        }

        private static void WriteBool4(ref JsonSerializer s, in bool4 value) {
            s.ElementBln(value.x);
            s.ElementBln(value.y);
            s.ElementBln(value.z);
            s.ElementBln(value.w);
        }

        public static void MemberBool2x2(this ref JsonSerializer s, in Str32 key, in bool2x2 value) {
            s.MemberArrayStart(key, true);
            ArrayBool2(ref s, in value.c0);
            ArrayBool2(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberBool2x3(this ref JsonSerializer s, in Str32 key, in bool2x3 value) {
            s.MemberArrayStart(key, true);
            ArrayBool2(ref s, in value.c0);
            ArrayBool2(ref s, in value.c1);
            ArrayBool2(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberBool2x4(this ref JsonSerializer s, in Str32 key, in bool2x4 value) {
            s.MemberArrayStart(key, true);
            ArrayBool2(ref s, in value.c0);
            ArrayBool2(ref s, in value.c1);
            ArrayBool2(ref s, in value.c2);
            ArrayBool2(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberBool3x2(this ref JsonSerializer s, in Str32 key, in bool3x2 value) {
            s.MemberArrayStart(key, true);
            ArrayBool3(ref s, in value.c0);
            ArrayBool3(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberBool3x3(this ref JsonSerializer s, in Str32 key, in bool3x3 value) {
            s.MemberArrayStart(key, true);
            ArrayBool3(ref s, in value.c0);
            ArrayBool3(ref s, in value.c1);
            ArrayBool3(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberBool3x4(this ref JsonSerializer s, in Str32 key, in bool3x4 value) {
            s.MemberArrayStart(key, true);
            ArrayBool3(ref s, in value.c0);
            ArrayBool3(ref s, in value.c1);
            ArrayBool3(ref s, in value.c2);
            ArrayBool3(ref s, in value.c3);
            s.ArrayEnd();
        }

        public static void MemberBool4x2(this ref JsonSerializer s, in Str32 key, in bool4x2 value) {
            s.MemberArrayStart(key, true);
            ArrayBool4(ref s, in value.c0);
            ArrayBool4(ref s, in value.c1);
            s.ArrayEnd();
        }

        public static void MemberBool4x3(this ref JsonSerializer s, in Str32 key, in bool4x3 value) {
            s.MemberArrayStart(key, true);
            ArrayBool4(ref s, in value.c0);
            ArrayBool4(ref s, in value.c1);
            ArrayBool4(ref s, in value.c2);
            s.ArrayEnd();
        }

        public static void MemberBool4x4(this ref JsonSerializer s, in Str32 key, in bool4x4 value) {
            s.MemberArrayStart(key, true);
            ArrayBool4(ref s, in value.c0);
            ArrayBool4(ref s, in value.c1);
            ArrayBool4(ref s, in value.c2);
            ArrayBool4(ref s, in value.c3);
            s.ArrayEnd();
        }
    }
}
