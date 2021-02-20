// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Unity.Mathematics;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst.Math.Tests
{
    public static partial class JsonMathTest
    {
        public static void MemberFloat2Temp(this ref JsonSerializer s, in Str32 key, in float2 value) {
            s.MemberArrayStart(key, false);
            WriteFloat2(ref s, in value);
            s.ArrayEnd();
        }
        
        public static void MemberFloat3Temp(this ref JsonSerializer s, in Str32 key, in float3 value) {
            s.MemberArrayStart(key, false);
            WriteFloat3(ref s, in value);
            s.ArrayEnd();
        }
        
        public static void MemberFloat4Temp(this ref JsonSerializer s, in Str32 key, in float4 value) {
            s.MemberArrayStart(key, false);
            WriteFloat4(ref s, in value);
            s.ArrayEnd();
        }
        
        public static void ArrayFloat2(this ref JsonSerializer s, in float2 value) {
            s.ArrayStart(false);
            WriteFloat2(ref s, in value);
            s.ArrayEnd();
        }
        
        public static void ArrayFloat3(this ref JsonSerializer s, in float3 value) {
            s.ArrayStart(false);
            WriteFloat3(ref s, in value);
            s.ArrayEnd();
        }
        
        public static void ArrayFloat4(this ref JsonSerializer s, in float4 value) {
            s.ArrayStart(false);
            WriteFloat4(ref s, in value);
            s.ArrayEnd();
        }
        
        private static void WriteFloat2(ref JsonSerializer s, in float2 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
        }
        
        private static void WriteFloat3(ref JsonSerializer s, in float3 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
        }
        
        private static void WriteFloat4(ref JsonSerializer s, in float4 value) {
            s.ElementDbl(value.x);
            s.ElementDbl(value.y);
            s.ElementDbl(value.z);
            s.ElementDbl(value.w);
        }
        
        // ----------------------
        public static void MemberFloat4x4Temp(this ref JsonSerializer s, in Str32 key, in float4x4 value) {
            s.MemberArrayStart(key, true);
            ArrayFloat4(ref s, in value.c0);
            ArrayFloat4(ref s, in value.c1);
            ArrayFloat4(ref s, in value.c2);
            ArrayFloat4(ref s, in value.c3);
            s.ArrayEnd();
        }
        
    }
}