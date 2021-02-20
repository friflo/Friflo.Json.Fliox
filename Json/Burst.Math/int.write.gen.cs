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
            public static void MemberInt2(this ref JsonSerializer s, in Str32 key, in int2 value) {
                s.MemberArrayStart(key, false);
                WriteInt2(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteInt2(ref JsonSerializer s, in int2 value) {
                s.ElementLng(value.x);
                s.ElementLng(value.y);
            }

            public static void MemberInt3(this ref JsonSerializer s, in Str32 key, in int3 value) {
                s.MemberArrayStart(key, false);
                WriteInt3(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteInt3(ref JsonSerializer s, in int3 value) {
                s.ElementLng(value.x);
                s.ElementLng(value.y);
                s.ElementLng(value.z);
            }

            public static void MemberInt4(this ref JsonSerializer s, in Str32 key, in int4 value) {
                s.MemberArrayStart(key, false);
                WriteInt4(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteInt4(ref JsonSerializer s, in int4 value) {
                s.ElementLng(value.x);
                s.ElementLng(value.y);
                s.ElementLng(value.z);
                s.ElementLng(value.w);
            }
    }
}
