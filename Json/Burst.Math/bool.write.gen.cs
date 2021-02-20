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

            private static void WriteBool2(ref JsonSerializer s, in bool2 value) {
                s.ElementBln(value.x);
                s.ElementBln(value.x);
            }

            public static void MemberBool3(this ref JsonSerializer s, in Str32 key, in bool3 value) {
                s.MemberArrayStart(key, false);
                WriteBool3(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteBool3(ref JsonSerializer s, in bool3 value) {
                s.ElementBln(value.x);
                s.ElementBln(value.x);
                s.ElementBln(value.x);
            }

            public static void MemberBool4(this ref JsonSerializer s, in Str32 key, in bool4 value) {
                s.MemberArrayStart(key, false);
                WriteBool4(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteBool4(ref JsonSerializer s, in bool4 value) {
                s.ElementBln(value.x);
                s.ElementBln(value.x);
                s.ElementBln(value.x);
                s.ElementBln(value.x);
            }
    }
}
