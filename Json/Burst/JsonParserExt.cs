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
        [Conditional("DEBUG")]
        private void AssertObject() {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectMember || curState == State.ExpectMemberFirst)
                return;
            throw new InvalidOperationException("Must call UseMember...() method only within an object");
        }
        
        public bool UseMemberObj(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberObj(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        // ---
        public bool UseMemberArr(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberArr(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNum(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNum(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberStr(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberStr(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberBln(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberBln(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNul(ref Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }

#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNul(Str32 name) {
            AssertObject();
            if (lastEvent != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void AssertArray() {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectElement || curState == State.ExpectElementFirst)
                return;
            throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj() {
            AssertArray();
            if (lastEvent != JsonEvent.ObjectStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementArr() {
            AssertArray();
            if (lastEvent != JsonEvent.ArrayStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementNum() {
            AssertArray();
            if (lastEvent != JsonEvent.ValueNumber)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementStr() {
            AssertArray();
            if (lastEvent != JsonEvent.ValueString)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementBln() {
            AssertArray();
            if (lastEvent != JsonEvent.ValueBool)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementNul() {
            AssertArray();
            if (lastEvent != JsonEvent.ValueNull)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ------------------------------------------------------------------------------------------------
        public bool NextObjectMember (ref ObjectIterator i) {
            if (i.hasIterated)  {
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState == State.ExpectMember) {
                    if (usedMember[level])
                        usedMember[level] = false; // clear found flag for next iteration
                    else
                        SkipEvent();
                } else {
                    throw new InvalidOperationException("NextObjectMember() - expect subsequent iteration being inside an object");
                }
            } else {
                if (lastEvent != JsonEvent.ObjectStart)
                    throw new InvalidOperationException("NextObjectMember() - expect initial iteration with an object (ObjectStart)");
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
                    throw new InvalidOperationException("unexpected ArrayEnd in JsonParser.NextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }
        
        public bool NextArrayElement (ref ArrayIterator i) {
            if (i.hasIterated){
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState == State.ExpectElement) {
                    if (usedMember[level])
                        usedMember[level] = false; // clear found flag for next iteration
                    else
                        SkipEvent();
                } else {
                    throw new InvalidOperationException("NextArrayElement() - expect subsequent iteration being inside an array");
                }
            } else {
                if (lastEvent != JsonEvent.ArrayStart)
                    throw new InvalidOperationException("NextArrayElement() - expect initial iteration with an array (ArrayStart)");
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
                    throw new InvalidOperationException("unexpected ObjectEnd in JsonParser.NextArrayElement()");
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