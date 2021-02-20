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
            public static void MemberDouble2(this ref JsonSerializer s, in Str32 key, in double2 value) {
                s.MemberArrayStart(key, false);
                WriteDouble2(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteDouble2(ref JsonSerializer s, in double2 value) {
                s.ElementDbl(value.x);
                s.ElementDbl(value.y);
            }

            public static void MemberDouble3(this ref JsonSerializer s, in Str32 key, in double3 value) {
                s.MemberArrayStart(key, false);
                WriteDouble3(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteDouble3(ref JsonSerializer s, in double3 value) {
                s.ElementDbl(value.x);
                s.ElementDbl(value.y);
                s.ElementDbl(value.z);
            }

            public static void MemberDouble4(this ref JsonSerializer s, in Str32 key, in double4 value) {
                s.MemberArrayStart(key, false);
                WriteDouble4(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteDouble4(ref JsonSerializer s, in double4 value) {
                s.ElementDbl(value.x);
                s.ElementDbl(value.y);
                s.ElementDbl(value.z);
                s.ElementDbl(value.w);
            }
    }
}
