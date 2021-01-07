// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Burst
{
    
    public struct ReadArray
    {
        private bool foundElement;
        private bool hasIterated;

        public override string ToString() {
            return $"{{ found: {foundElement} }}";
        }

        public bool ContinueArray(ref JsonParser parser) {
            if (!foundElement && hasIterated) {
                parser.SkipEvent();
            }
            hasIterated = true;
            foundElement = false;
            return parser.ContinueArray();
        }
        
        public bool UseObj(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ObjectStart) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseArr(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ArrayStart) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNum(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ValueNumber){
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseStr(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ValueString) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseBln(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ValueBool) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNul(ref JsonParser parser) {
            if (!foundElement && parser.ev == JsonEvent.ValueNull) {
                foundElement = true;
                return true;
            }
            return false;
        }

    }
}