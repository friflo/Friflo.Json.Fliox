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
            if (Event == JsonEvent.ObjectStart || Event == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectMember || curState == State.ExpectMemberFirst)
                return;
            throw new InvalidOperationException("Must call UseMember...() method only within an object");
        }
        
        public bool UseMemberObj(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseMemberObj(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        // ---
        public bool UseMemberArr(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseMemberArr(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNum(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberNum(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberStr(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberStr(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberBln(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberBln(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNul(ref Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberNul(Str32 name) {
            AssertObject();
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ----------- array element checks -----------
        [Conditional("DEBUG")]
        private void AssertArray() {
            int level = stateLevel;
            if (Event == JsonEvent.ObjectStart || Event == JsonEvent.ArrayStart)
                level--;
            State curState = state[level];
            if (curState == State.ExpectElement || curState == State.ExpectElementFirst)
                return;
            throw new InvalidOperationException("Must call UseElement...() method on within an array");
        }
        
        public bool UseElementObj() {
            AssertArray();
            if (Event != JsonEvent.ObjectStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementArr() {
            AssertArray();
            if (Event != JsonEvent.ArrayStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementNum() {
            AssertArray();
            if (Event != JsonEvent.ValueNumber)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementStr() {
            AssertArray();
            if (Event != JsonEvent.ValueString)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementBln() {
            AssertArray();
            if (Event != JsonEvent.ValueBool)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementNul() {
            AssertArray();
            if (Event != JsonEvent.ValueNull)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
    }
}