// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System; 
using System.Diagnostics; 
using static Friflo.Json.Burst.JsonParser;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Burst
{
    public ref struct JObj
    {
        private readonly   int     expectedLevel;  // todo exclude in RELEASE
        private            bool    hasIterated;
        private            bool    usedMember;
        
        internal JObj(int level) {

            expectedLevel   = level;
            hasIterated     = false;
            usedMember      = false;
        }
        
        public bool NextObjectMember (ref JsonParser p) { // , Skip skip
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
                // if (skip == Skip.Auto) {
                if (usedMember) {
                    usedMember = false; // clear found flag for next iteration
                } else {
                    if (!p.SkipEvent())
                        return false;
                }
                // }
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
        
        // ----------- object member checks -----------
        [Conditional("DEBUG")]
        private void UseMember(ref JsonParser p) {
            if (!hasIterated)
                throw new InvalidOperationException("Must call UseMember...() only after NextObjectMember()");
                
            int level = p.stateLevel;
            if (p.lastEvent == JsonEvent.ObjectStart || p.lastEvent == JsonEvent.ArrayStart)
                level--;
            if (level != expectedLevel)
                throw new InvalidOperationException("Unexpected level in UseMember...() method");
            State curState = p.state.array[level];
            if (curState != State.ExpectMember)
                throw new InvalidOperationException("Must call UseMember...() method only within an object");
        }
        
        public bool UseMemberObj(ref JsonParser p, in Str32 name, out JObj obj) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ObjectStart || !p.key.IsEqual32(in name)) {
                obj = new JObj(-1);
                return false;
            }
            obj = new JObj(p.stateLevel);
            usedMember = true;
            return true;
        }
        
        public bool UseMemberArr(ref JsonParser p, in Str32 name, out JArr arr) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ArrayStart || !p.key.IsEqual32(in name)) {
                arr = new JArr(-1);
                return false;
            }
            usedMember = true;
            arr = new JArr(p.stateLevel);
            return true;
        }
        
        public bool UseMemberNum(ref JsonParser p, in Str32 name) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ValueNumber || !p.key.IsEqual32(in name))
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseMemberStr(ref JsonParser p, in Str32 name) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ValueString || !p.key.IsEqual32(in name))
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseMemberBln(ref JsonParser p, in Str32 name) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ValueBool || !p.key.IsEqual32(in name))
                return false;
            usedMember = true;
            return true;
        }
        
        public bool UseMemberNul(ref JsonParser p, in Str32 name) {
            UseMember(ref p);
            if (p.lastEvent != JsonEvent.ValueNull || !p.key.IsEqual32(in name)) 
                return false;
            usedMember = true;
            return true;
        }
    }
}
