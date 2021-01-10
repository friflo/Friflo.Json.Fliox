// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System; // Required for [Obsolete] 

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
        private bool UseMember(ref ObjectIterator iterator, JsonEvent expect, ref Str32 name) {
#if DEBUG
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.level)
                throw new InvalidOperationException("Unexpected level in UseMember...() method. ");
            State curState = state[level];
            if (curState != State.ExpectMember)
                throw new InvalidOperationException("Must call UseMember...() method only within an object");
#endif
            if (lastEvent != expect || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberObj(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ObjectStart, ref name);
        }
        public bool UseMemberObj(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ObjectStart, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberArr(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ArrayStart, ref name);
        }
        public bool UseMemberArr(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ArrayStart, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNum(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueNumber, ref name);
        }
        public bool UseMemberNum(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueNumber, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberStr(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueString, ref name);
        }
        public bool UseMemberStr(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueString, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberBln(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueBool, ref name);
        }
        public bool UseMemberBln(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueBool, ref name);
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNul(ref ObjectIterator iterator, Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueNull, ref name);
        }
        public bool UseMemberNul(ref ObjectIterator iterator, ref Str32 name) {
            return UseMember(ref iterator, JsonEvent.ValueNull, ref name);
        }

        // ----------- array element checks -----------
        private bool UseElement(ref ArrayIterator iterator, JsonEvent expect) {
#if DEBUG
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.level)
                throw new InvalidOperationException("Unexpected level in UseElement...() method. ");
            State curState = state[level];
            if (curState != State.ExpectElement)
                throw new InvalidOperationException("Must call UseElement...() method on within an array");
#endif
            if (lastEvent != expect)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementObj(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ObjectStart);
        }
        
        public bool UseElementArr(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ArrayStart);
        }
        
        public bool UseElementNum(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ValueNumber);
        }
        
        public bool UseElementStr(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ValueString);
        }
        
        public bool UseElementBln(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ValueBool);
        }
        
        public bool UseElementNul(ref ArrayIterator iterator) {
            return UseElement(ref iterator, JsonEvent.ValueNull);
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
                    if (i.usedMember) {
                        i.usedMember = false; // clear found flag for next iteration
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
                    if (i.usedMember) {
                        i.usedMember = false; // clear found flag for next iteration
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

        public ObjectIterator GetObjectIterator() {
#if DEBUG
            if (lastEvent != JsonEvent.ObjectStart)
                throw new InvalidOperationException("Expect ObjectStart in GetObjectIterator()");
#endif
            return new ObjectIterator(stateLevel);
        }

        public ArrayIterator GetArrayIterator() {
#if DEBUG
            if (lastEvent != JsonEvent.ArrayStart)
                throw new InvalidOperationException("Expect ArrayStart in GetObjectIterator()");
#endif
            return new ArrayIterator(stateLevel);
        }
    }

    public ref struct ObjectIterator
    {
        internal ObjectIterator(int level) {

            this.level = level;
            hasIterated = false;
            usedMember = false;
        }
        internal readonly   int level;
        internal            bool hasIterated;
        internal            bool usedMember;
    }
    
    public ref struct ArrayIterator {
        internal ArrayIterator(int level) {

            this.level = level;
            hasIterated = false;
            usedMember = false;
        }
        internal readonly   int level;
        internal            bool hasIterated;
        internal            bool usedMember;
    }
}