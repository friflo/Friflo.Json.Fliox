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
        private JsonEvent ev;
        private bool foundElement;
        private bool hasIterated;

        public override string ToString() {
            return $"{{ event: {ev}, found: {foundElement} }}";
        }

        public bool NextEvent(ref JsonParser parser) {
            if (!foundElement && hasIterated) {
                parser.SkipEvent(ev);
            }
            ev = parser.NextEvent();
            hasIterated = true;
            foundElement = false;
            return parser.ContinueArray(ev);
        }
        
        public bool IsObj(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ObjectStart) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool IsArr(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ArrayStart) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool IsNum(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueNumber){
                foundElement = true;
                return true;
            }
            return false;
        }
		
        public bool IsStr(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueString) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool IsBln(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueBool) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool IsNul(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueNull) {
                foundElement = true;
                return true;
            }
            return false;
        }

    }
}