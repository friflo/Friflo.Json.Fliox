// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    
    public struct ReadObject
    {
        public JsonEvent ev;
        private bool foundMember;
        private bool hasIterated;
        
        public override string ToString() {
            return $"{{ event: {ev}, found: {foundMember} }}";
        }

        public bool NextEvent(ref JsonParser parser) {
            if (!foundMember && hasIterated) {
                parser.SkipEvent(ev);
            }
            ev = parser.NextEvent();
            hasIterated = true;
            foundMember = false;
            return parser.ContinueObject(ev);
        }

        public bool IsObj(ref JsonParser parser, Str32 name) {
            return IsObj(ref parser, ref name);
        }

        public bool IsObj(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ObjectStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool IsArr(ref JsonParser parser, Str32 name) {
            return IsArr(ref parser, ref name);
        }

        public bool IsArr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ArrayStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool IsNum(ref JsonParser parser, Str32 name) {
            return IsNum(ref parser, ref name);
        }

        public bool IsNum(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueNumber && parser.key.IsEqual32(ref name)){
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool IsStr(ref JsonParser parser, Str32 name) {
            return IsStr(ref parser, ref name);
        }

        public bool IsStr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueString && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool IsBln(ref JsonParser parser, Str32 name) {
            return IsBln(ref parser, ref name);
        }

        public bool IsBln(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueBool && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool IsNul(ref JsonParser parser, Str32 name) {
            return IsNul(ref parser, ref name);
        }

        public bool IsNul(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueNull && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

    }
}