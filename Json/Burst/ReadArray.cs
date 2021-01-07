// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

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
            if (hasIterated) {
                if (!foundElement) 
                    parser.SkipEvent();
            }
            else {
                if (parser.Event != JsonEvent.ArrayStart)
                    throw new InvalidOperationException("ReadArray.ContinueArray() - expect ArrayStart on first entry");
            }
            hasIterated = true;
            foundElement = false;
            return parser.ContinueArray();
        }
        
        public bool UseObj(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ObjectStart) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseArr(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ArrayStart) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNum(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ValueNumber){
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseStr(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ValueString) {
                foundElement = true;
                return true;
            }
            return false;
        }
        
        public bool UseBln(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ValueBool) {
                foundElement = true;
                return true;
            }
            return false;
        }

        public bool UseNul(ref JsonParser parser) {
            if (!foundElement && parser.Event == JsonEvent.ValueNull) {
                foundElement = true;
                return true;
            }
            return false;
        }

    }
}