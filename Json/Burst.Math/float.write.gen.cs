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
            public static void MemberFloat2(this ref JsonSerializer s, in Str32 key, in float2 value) {
                s.MemberArrayStart(key, false);
                WriteFloat2(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteFloat2(ref JsonSerializer s, in float2 value) {
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
            }

            public static void MemberFloat3(this ref JsonSerializer s, in Str32 key, in float3 value) {
                s.MemberArrayStart(key, false);
                WriteFloat3(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteFloat3(ref JsonSerializer s, in float3 value) {
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
            }

            public static void MemberFloat4(this ref JsonSerializer s, in Str32 key, in float4 value) {
                s.MemberArrayStart(key, false);
                WriteFloat4(ref s, in value);
                s.ArrayEnd();
            }

            private static void WriteFloat4(ref JsonSerializer s, in float4 value) {
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
                s.ElementFlt(value.x);
            }
    }
}
