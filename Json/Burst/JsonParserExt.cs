// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System; // Required for [Obsolete] 
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
        private void UseMember(ref ObjectIterator iterator) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.level)
                throw new InvalidOperationException("Unexpected level in UseMember...() method");
            State curState = state[level];
            if (curState != State.ExpectMember)
                throw new InvalidOperationException("Must call UseMember...() method only within an object");
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberObj(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberObj(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberArr(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberArr(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNum(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberNum(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberStr(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberStr(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberBln(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberBln(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
#if JSON_BURST
        [Obsolete("Performance degradation by string copy > to avoid use the (ref FixedString32) version", false)]
#endif
        public bool UseMemberNul(ref ObjectIterator iterator, Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        public bool UseMemberNul(ref ObjectIterator iterator, ref Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            iterator.usedMember = true;
            return true;
        }

        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void UseElement(ref ArrayIterator iterator) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.level)
                throw new InvalidOperationException("Unexpected level in UseElement...() method");
            State curState = state[level];
            if (curState != State.ExpectElement)
                throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ObjectStart)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementArr(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ArrayStart)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementNum(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueNumber)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementStr(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueString)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementBln(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueBool)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementNul(ref ArrayIterator iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueNull)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        // ------------------------------------------------------------------------------------------------
        public bool NextObjectMember (ref ObjectIterator i) {
            if (lastEvent == JsonEvent.Error)
                return false;
            
            if (i.hasIterated)  {
#if DEBUG
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState != State.ExpectMember)
                    throw new InvalidOperationException("NextObjectMember() - expect subsequent iteration being inside an object");
#endif
                if (i.usedMember) {
                    i.usedMember = false; // clear found flag for next iteration
                } else {
                    if (!SkipEvent())
                        return false;
                }
            } else {
                // assertion is cheap -> throw exception also in DEBUG & RELEASE
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
                    throw new InvalidOperationException ("unexpected ArrayEnd in NextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }
        
        public bool NextArrayElement (ref ArrayIterator i) {
            if (lastEvent == JsonEvent.Error)
                return false;
            
            if (i.hasIterated) {
#if DEBUG
                int level = stateLevel;
                if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                    level--;
                State curState = state[level];
                if (curState != State.ExpectElement) 
                    throw new InvalidOperationException("NextArrayElement() - expect subsequent iteration being inside an array");
#endif
                if (i.usedMember) {
                    i.usedMember = false; // clear found flag for next iteration
                }
                else {
                    if (!SkipEvent())
                        return false;
                }
            } else {
                // assertion is cheap -> throw exception also in DEBUG & RELEASE
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
                    // assertion is cheap -> throw exception also in DEBUG & RELEASE
                    throw new InvalidOperationException("unexpected ObjectEnd in NextArrayElement()");
            }
            return false;
        }

        public ObjectIterator GetObjectIterator() {
            // assertion is cheap -> throw exception also in DEBUG & RELEASE
            if (lastEvent != JsonEvent.ObjectStart)
                throw new InvalidOperationException("Expect ObjectStart in GetObjectIterator()");
            return new ObjectIterator(stateLevel);
        }

        public ArrayIterator GetArrayIterator() {
            // assertion is cheap -> throw exception also in DEBUG & RELEASE
            if (lastEvent != JsonEvent.ArrayStart)
                throw new InvalidOperationException("Expect ArrayStart in GetObjectIterator()");
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