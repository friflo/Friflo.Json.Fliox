// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System; // Required for [Obsolete] 
using System.Diagnostics; 

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
    // ReSharper disable InconsistentNaming
#endif

using static Friflo.Json.Burst.JsonParser;

namespace Friflo.Json.Burst
{
    public enum Skip {
        No,
        Auto
    }
    
    public partial struct JsonParser
    {
        public bool IsRootArray(out JArr arr) {
            if (stateLevel != 1)
                throw new InvalidOperationException("UseRootObject() is only applicable to JSON root");
            if (lastEvent != JsonEvent.ArrayStart) {
                arr = new JArr(-1);
                return false;
            }
            arr = new JArr(stateLevel);
            return true;
        }
    }
   
    // ------------------------------------------------------------------------------------------------
    public ref struct JArr {
        private readonly   int     expectedLevel;  // todo exclude in RELEASE
        private            bool    hasIterated;
        private            bool    usedMember;
        
        internal JArr(int level) {

            this.expectedLevel = level;
            hasIterated = false;
            usedMember = false;
        }

        public bool NextArrayElement(ref JsonParser p) {
            return NextArrayElement(ref p, Skip.Auto);
        }
        
        public bool NextArrayElement (ref JsonParser p, Skip skip) {
            if (p.lastEvent == JsonEvent.Error)
                return false;
            
            if (hasIterated) {
#if DEBUG
                int level = p.stateLevel;
                if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                    level--;
                if (level != expectedLevel)
                    throw new InvalidOperationException("Unexpected iterator level in NextArrayElement()");
                State curState = p.state.array[level];
                if (curState != State.ExpectElement) 
                    throw new InvalidOperationException("NextArrayElement() - expect subsequent iteration being inside an array");
#endif
                if (skip == Skip.Auto) {
                    if (usedMember) {
                        usedMember = false; // clear found flag for next iteration
                    }
                    else {
                        if (!p.SkipEvent())
                            return false;
                    }
                }
            } else {
                // assertion is cheap -> throw exception also in DEBUG & RELEASE
                if (p.lastEvent != JsonEvent.ArrayStart)
                    throw new InvalidOperationException("NextArrayElement() - expect initial iteration with an array (ArrayStart)");
                hasIterated = true;
            }
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    break;
                case JsonEvent.ObjectEnd:
                    // assertion is cheap -> throw exception also in DEBUG & RELEASE
                    throw new InvalidOperationException("unexpected ObjectEnd in NextArrayElement()");
            }
            return false;
        }
        
        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void UseElement(ref JsonParser p) {
            if (!hasIterated)
                throw new InvalidOperationException("Must call UseElement...() only after NextArrayElement()");

            int level = p.stateLevel;
            if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != expectedLevel)
                throw new InvalidOperationException("Unexpected iterator level in UseElement...() method");
            State curState = p.state.array[level];
            if (curState != State.ExpectElement)
                throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj(ref JsonParser p, out JObj obj) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ObjectStart) {
                obj = new JObj(-1);
                return false;
            }
            usedMember = true;
            obj = new JObj(p.stateLevel);
            return true;
        }
        
        public bool UseElementArr(ref JsonParser p, out JArr arr) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ArrayStart) {
                arr = new JArr(-1);
                return false;
            }
            usedMember = true;
            arr = new JArr(p.stateLevel);
            return true;
        }
        
        public bool UseElementNum(ref JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueNumber)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementStr(ref JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueString)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementBln(ref JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueBool)
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseElementNul(ref JsonParser p) {
            UseElement(ref p);
            if (p.lastEvent != JsonEvent.ValueNull)
                return false;
            usedMember = true;
            return true;
        }
    }
}
