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
        private JsonEvent ev; // Note: Dont make public! The intention is to use Use...() which support auto skipping
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

        public bool UseObj(ref JsonParser parser, Str32 name) {
            return UseObj(ref parser, ref name);
        }

        public bool UseObj(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ObjectStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseArr(ref JsonParser parser, Str32 name) {
            return UseArr(ref parser, ref name);
        }

        public bool UseArr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ArrayStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseNum(ref JsonParser parser, Str32 name) {
            return UseNum(ref parser, ref name);
        }

        public bool UseNum(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueNumber && parser.key.IsEqual32(ref name)){
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseStr(ref JsonParser parser, Str32 name) {
            return UseStr(ref parser, ref name);
        }

        public bool UseStr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueString && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseBln(ref JsonParser parser, Str32 name) {
            return UseBln(ref parser, ref name);
        }

        public bool UseBln(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueBool && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseNul(ref JsonParser parser, Str32 name) {
            return UseNul(ref parser, ref name);
        }

        public bool UseNul(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && ev == JsonEvent.ValueNull && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

    }
}