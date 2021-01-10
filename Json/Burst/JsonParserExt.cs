// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;

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
        private bool UseMember(JsonEvent expect, ref Str32 name) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectMember) {
                if (lastEvent != expect || !key.IsEqual32(name))
                    return false;
                usedMember[level] = true;
                return true;
            }
            return SetApplicationError("Must call UseMember...() method only within an object");
        }
        
        public bool UseMemberObj(ref Str32 name) {
            return UseMember(JsonEvent.ObjectStart, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberObj(Str32 name) {
            return UseMember(JsonEvent.ObjectStart, ref name);
        }
        
        // ---
        public bool UseMemberArr(ref Str32 name) {
            return UseMember(JsonEvent.ArrayStart, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberArr(Str32 name) {
            return UseMember(JsonEvent.ArrayStart, ref name);
        }
        
        // ---
        public bool UseMemberNum(ref Str32 name) {
            return UseMember(JsonEvent.ValueNumber, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNum(Str32 name) {
            return UseMember(JsonEvent.ValueNumber, ref name);
        }
        
        // ---
        public bool UseMemberStr(ref Str32 name) {
            return UseMember(JsonEvent.ValueString, ref name);
        }
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberStr(Str32 name) {
            return UseMember(JsonEvent.ValueString, ref name);
        }
        
        // ---
        public bool UseMemberBln(ref Str32 name) {
            return UseMember(JsonEvent.ValueBool, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberBln(Str32 name) {
            return UseMember(JsonEvent.ValueBool, ref name);
        }
        
        // ---
        public bool UseMemberNul(ref Str32 name) {
            return UseMember(JsonEvent.ValueNull, ref name);
        }

#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNul(Str32 name) {
            return UseMember(JsonEvent.ValueNull, ref name);
        }
        
        // ----------- array element checks -----------
        private bool UseElement(JsonEvent expect) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectElement) {
                if (lastEvent != expect)
                    return false;
                usedMember[level] = true;
                return true;
            }
            return SetApplicationError("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj() {
            return UseElement(JsonEvent.ObjectStart);
        }
        
        public bool UseElementArr() {
            return UseElement(JsonEvent.ArrayStart);
        }
        
        public bool UseElementNum() {
            return UseElement(JsonEvent.ValueNumber);
        }
        
        public bool UseElementStr() {
            return UseElement(JsonEvent.ValueString);
        }
        
        public bool UseElementBln() {
            return UseElement(JsonEvent.ValueBool);
        }
        
        public bool UseElementNul() {
            return UseElement(JsonEvent.ValueNull);
        }
        
        // ------------------------------------------------------------------------------------------------
        public bool NextObjectMember (ref ObjectIterator i) {
            if (lastEvent == JsonEvent.Error)
                return false;
            
            if (i.hasIterated)  {
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState == State.ExpectMember) {
                    if (usedMember[level]) {
                        usedMember[level] = false; // clear found flag for next iteration
                    } else {
                        if (!SkipEvent())
                            return false;
                    }
                } else {
                    return SetApplicationError("NextObjectMember() - expect subsequent iteration being inside an object");
                }
            } else {
                if (lastEvent != JsonEvent.ObjectStart)
                    return SetApplicationError("NextObjectMember() - expect initial iteration with an object (ObjectStart)");
                i.hasIterated = true;
            }
            JsonEvent ev = NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    return SetApplicationError ("unexpected ArrayEnd in NextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }
        
        public bool NextArrayElement (ref ArrayIterator i) {
            if (lastEvent == JsonEvent.Error)
                return false;
            
            if (i.hasIterated){
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState == State.ExpectElement) {
                    if (usedMember[level]) {
                        usedMember[level] = false; // clear found flag for next iteration
                    } else {
                        if (!SkipEvent())
                            return false;
                    }
                } else {
                    return SetApplicationError("NextArrayElement() - expect subsequent iteration being inside an array");
                }
            } else {
                if (lastEvent != JsonEvent.ArrayStart)
                    return SetApplicationError("NextArrayElement() - expect initial iteration with an array (ArrayStart)");
                i.hasIterated = true;
            }
            JsonEvent ev = NextEvent();
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
                    return SetApplicationError("unexpected ObjectEnd in NextArrayElement()");
            }
            return false;
        }
    }

    public struct ObjectIterator {
        public bool hasIterated;
    }
    
    public struct ArrayIterator {
        public bool hasIterated;
    }
}