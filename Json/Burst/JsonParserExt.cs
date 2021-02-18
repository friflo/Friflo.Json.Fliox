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
        // ----------- object member checks -----------
        [Conditional("DEBUG")]
        private void UseMember(ref JObj iterator) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.expectedLevel)
                throw new InvalidOperationException("Unexpected level in UseMember...() method");
            State curState = state.array[level];
            if (curState != State.ExpectMember)
                throw new InvalidOperationException("Must call UseMember...() method only within an object");
        }
        
        public bool UseMemberObj(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ObjectStart || !key.IsEqual32(in name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseMemberArr(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ArrayStart || !key.IsEqual32(in name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseMemberNum(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNumber || !key.IsEqual32(in name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseMemberStr(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueString || !key.IsEqual32(in name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseMemberBln(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueBool || !key.IsEqual32(in name))
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseMemberNul(ref JObj iterator, in Str32 name) {
            UseMember(ref iterator);
            if (lastEvent != JsonEvent.ValueNull || !key.IsEqual32(in name)) 
                return false;
            iterator.usedMember = true;
            return true;
        }

        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void UseElement(ref JArr iterator) {
            int level = stateLevel;
            if (lastEvent == JsonEvent.ObjectStart || lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != iterator.expectedLevel)
                throw new InvalidOperationException("Unexpected iterator level in UseElement...() method");
            State curState = state.array[level];
            if (curState != State.ExpectElement)
                throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ObjectStart)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementArr(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ArrayStart)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementNum(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueNumber)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementStr(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueString)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementBln(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueBool)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        public bool UseElementNul(ref JArr iterator) {
            UseElement(ref iterator);
            if (lastEvent != JsonEvent.ValueNull)
                return false;
            iterator.usedMember = true;
            return true;
        }
        
        // ------------------------------------------------------------------------------------------------

        


        public JObj GetObjectIterator() {
            // assertion is cheap -> throw exception also in DEBUG & RELEASE
            if (lastEvent != JsonEvent.ObjectStart)
                throw new InvalidOperationException("Expect ObjectStart in GetObjectIterator()");
            return new JObj(stateLevel);
        }

        public JArr GetArrayIterator() {
            // assertion is cheap -> throw exception also in DEBUG & RELEASE
            if (lastEvent != JsonEvent.ArrayStart)
                throw new InvalidOperationException("Expect ArrayStart in GetArrayIterator()");
            return new JArr(stateLevel);
        }
    }

    public ref struct JObj
    {
        internal readonly   int     expectedLevel;  // todo exclude in RELEASE
        internal            bool    hasIterated;
        internal            bool    usedMember;
        
        internal JObj(int level) {

            expectedLevel   = level;
            hasIterated     = false;
            usedMember      = false;
        }
        
        public bool NextObjectMember(ref JsonParser p) {
            return NextObjectMember(ref p, Skip.Auto);
        }

        public bool NextObjectMember (ref JsonParser p, Skip skip) {
            if (p.lastEvent == JsonEvent.Error)
                return false;
            
            if (hasIterated)  {
#if DEBUG
                int level = p.stateLevel;
                if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                    level--;
                if (level != expectedLevel)
                    throw new InvalidOperationException("Unexpected iterator level in NextObjectMember()");
                State curState = p.state.array[level];
                if (curState != State.ExpectMember)
                    throw new InvalidOperationException("NextObjectMember() - expect subsequent iteration being inside an object");
#endif
                if (skip == Skip.Auto) {
                    if (usedMember) {
                        usedMember = false; // clear found flag for next iteration
                    } else {
                        if (!p.SkipEvent())
                            return false;
                    }
                }
            } else {
                // assertion is cheap -> throw exception also in DEBUG & RELEASE
                if (p.lastEvent != JsonEvent.ObjectStart)
                    throw new InvalidOperationException("NextObjectMember() - expect initial iteration with an object (ObjectStart)");
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
                    throw new InvalidOperationException ("unexpected ArrayEnd in NextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }

    }
    
    public ref struct JArr {
        internal readonly   int     expectedLevel;  // todo exclude in RELEASE
        internal            bool    hasIterated;
        internal            bool    usedMember;
        
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
    }
}