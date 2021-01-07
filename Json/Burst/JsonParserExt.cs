// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    public partial struct JsonParser
    {
        // ----------- object member checks -----------
        public bool IsMemberObj(ref Str32 name) {
            return ev == JsonEvent.ObjectStart && key.IsEqual32(name);
        }
        
        public bool IsMemberObj(Str32 name) {
            return ev == JsonEvent.ObjectStart && key.IsEqual32(name);
        }
        // ---
        public bool IsMemberArr(ref Str32 name) {
            return ev == JsonEvent.ArrayStart && key.IsEqual32(name);
        }
        
        public bool IsMemberArr(Str32 name) {
            return ev == JsonEvent.ArrayStart && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberNum(ref Str32 name) {
            return ev == JsonEvent.ValueNumber && key.IsEqual32(name);
        }
        
        public bool IsMemberNum(Str32 name) {
            return ev == JsonEvent.ValueNumber && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberStr(ref Str32 name) {
            return ev == JsonEvent.ValueString && key.IsEqual32(name);
        }
        
        public bool IsMemberStr(Str32 name) {
            return ev == JsonEvent.ValueString && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberBln(ref Str32 name) {
            return ev == JsonEvent.ValueBool && key.IsEqual32(name);
        }
        
        public bool IsMemberBln(Str32 name) {
            return ev == JsonEvent.ValueBool && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberNul(ref Str32 name) {
            return ev == JsonEvent.ValueNull && key.IsEqual32(name);
        }
        
        public bool IsMemberNul(Str32 name) {
            return ev == JsonEvent.ValueNull && key.IsEqual32(name);
        }
        
        // ----------- array element checks -----------
        public bool IsElementObj() {
            return ev == JsonEvent.ObjectStart;
        }
        
        public bool IsElementObj(Str32 name) {
            return ev == JsonEvent.ObjectStart;
        }
        
        public bool IsElementArr() {
            return ev == JsonEvent.ArrayStart;
        }
        
        public bool IsElementArr(Str32 name) {
            return ev == JsonEvent.ArrayStart;
        }
        
        public bool IsElementNum() {
            return ev == JsonEvent.ValueNumber;
        }
        
        public bool IsElementStr() {
            return ev == JsonEvent.ValueString;
        }
        
        public bool IsElementBln() {
            return ev == JsonEvent.ValueBool;
        }
        
        public bool IsElementNul() {
            return ev == JsonEvent.ValueNull;
        }
    }
}