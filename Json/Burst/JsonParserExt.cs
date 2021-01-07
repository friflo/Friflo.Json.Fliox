// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif
    using System;

namespace Friflo.Json.Burst
{
    public partial struct JsonParser
    {
        // ----------- object member checks -----------
        public bool UseMemberObj(ref Str32 name) {
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseMemberObj(Str32 name) {
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        // ---
        public bool UseMemberArr(ref Str32 name) {
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseMemberArr(Str32 name) {
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNum(ref Str32 name) {
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberNum(Str32 name) {
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberStr(ref Str32 name) {
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberStr(Str32 name) {
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberBln(ref Str32 name) {
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberBln(Str32 name) {
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public bool UseMemberNul(ref Str32 name) {
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseMemberNul(Str32 name) {
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        // ---
        public void UseMember() {
            switch (Event) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                    usedMember[stateLevel] = true;
                    break;
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    usedMember[stateLevel - 1] = true;
                    break;
            }
        }

        // ----------- array element checks -----------
        public bool UseElementObj() {
            if (Event != JsonEvent.ObjectStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementObj(Str32 name) {
            if (Event != JsonEvent.ObjectStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementArr() {
            if (Event != JsonEvent.ArrayStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementArr(Str32 name) {
            if (Event != JsonEvent.ArrayStart)
                return false;
            usedMember[stateLevel - 1] = true;
            return true;
        }
        
        public bool UseElementNum() {
            if (Event != JsonEvent.ValueNumber)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementStr() {
            if (Event != JsonEvent.ValueString)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementBln() {
            if (Event != JsonEvent.ValueBool)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }
        
        public bool UseElementNul() {
            if (Event != JsonEvent.ValueNull)
                return false;
            usedMember[stateLevel] = true;
            return true;
        }

        public void UseElement() {
            UseMember();
        }
    }
}