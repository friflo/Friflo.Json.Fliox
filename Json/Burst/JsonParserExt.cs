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
            return Event == JsonEvent.ObjectStart && key.IsEqual32(name);
        }
        
        public bool IsMemberObj(Str32 name) {
            return Event == JsonEvent.ObjectStart && key.IsEqual32(name);
        }
        // ---
        public bool IsMemberArr(ref Str32 name) {
            return Event == JsonEvent.ArrayStart && key.IsEqual32(name);
        }
        
        public bool IsMemberArr(Str32 name) {
            return Event == JsonEvent.ArrayStart && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberNum(ref Str32 name) {
            return Event == JsonEvent.ValueNumber && key.IsEqual32(name);
        }
        
        public bool IsMemberNum(Str32 name) {
            return Event == JsonEvent.ValueNumber && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberStr(ref Str32 name) {
            return Event == JsonEvent.ValueString && key.IsEqual32(name);
        }
        
        public bool IsMemberStr(Str32 name) {
            return Event == JsonEvent.ValueString && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberBln(ref Str32 name) {
            return Event == JsonEvent.ValueBool && key.IsEqual32(name);
        }
        
        public bool IsMemberBln(Str32 name) {
            return Event == JsonEvent.ValueBool && key.IsEqual32(name);
        }
        
        // ---
        public bool IsMemberNul(ref Str32 name) {
            return Event == JsonEvent.ValueNull && key.IsEqual32(name);
        }
        
        public bool IsMemberNul(Str32 name) {
            return Event == JsonEvent.ValueNull && key.IsEqual32(name);
        }
        
        // ----------- array element checks -----------
        public bool IsElementObj() {
            return Event == JsonEvent.ObjectStart;
        }
        
        public bool IsElementObj(Str32 name) {
            return Event == JsonEvent.ObjectStart;
        }
        
        public bool IsElementArr() {
            return Event == JsonEvent.ArrayStart;
        }
        
        public bool IsElementArr(Str32 name) {
            return Event == JsonEvent.ArrayStart;
        }
        
        public bool IsElementNum() {
            return Event == JsonEvent.ValueNumber;
        }
        
        public bool IsElementStr() {
            return Event == JsonEvent.ValueString;
        }
        
        public bool IsElementBln() {
            return Event == JsonEvent.ValueBool;
        }
        
        public bool IsElementNul() {
            return Event == JsonEvent.ValueNull;
        }
    }
}