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
        private bool foundMember;
        private bool hasIterated;
        
        public override string ToString() {
            return $"{{ found: {foundMember} }}";
        }

        public bool ContinueObject(ref JsonParser parser) {
            if (!foundMember && hasIterated) {
                parser.SkipEvent();
            }
            hasIterated = true;
            foundMember = false;
            return parser.ContinueObject();
        }

        public bool UseObj(ref JsonParser parser, Str32 name) {
            return UseObj(ref parser, ref name);
        }

        public bool UseObj(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ObjectStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseArr(ref JsonParser parser, Str32 name) {
            return UseArr(ref parser, ref name);
        }

        public bool UseArr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ArrayStart && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseNum(ref JsonParser parser, Str32 name) {
            return UseNum(ref parser, ref name);
        }

        public bool UseNum(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ValueNumber && parser.key.IsEqual32(ref name)){
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseStr(ref JsonParser parser, Str32 name) {
            return UseStr(ref parser, ref name);
        }

        public bool UseStr(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ValueString && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseBln(ref JsonParser parser, Str32 name) {
            return UseBln(ref parser, ref name);
        }

        public bool UseBln(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ValueBool && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

        public bool UseNul(ref JsonParser parser, Str32 name) {
            return UseNul(ref parser, ref name);
        }

        public bool UseNul(ref JsonParser parser, ref Str32 name) {
            if (!foundMember && parser.ev == JsonEvent.ValueNull && parser.key.IsEqual32(ref name)) {
                foundMember = true;
                return true;
            }
            return false;
        }

    }
}