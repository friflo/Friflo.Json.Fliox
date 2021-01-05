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
        private JsonEvent ev; // Note: Dont make public! The intention is to use Use...() which support auto skipping
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
        
        public bool UseObj(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ObjectStart) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseArr(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ArrayStart) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNum(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueNumber){
                foundElement = true;
                return true;
            }
            return false;
        }
		
        public bool UseStr(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueString) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseBln(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueBool) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNul(ref JsonParser parser) {
            if (!foundElement && ev == JsonEvent.ValueNull) {
                foundElement = true;
                return true;
            }
            return false;
        }

    }
}