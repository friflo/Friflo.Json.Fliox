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
        public bool IsMemberObj(ref Str32 name) {
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberObj(Str32 name) {
            if (Event != JsonEvent.ObjectStart || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        // ---
        public bool IsMemberArr(ref Str32 name) {
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberArr(Str32 name) {
            if (Event != JsonEvent.ArrayStart || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        // ---
        public bool IsMemberNum(ref Str32 name) {
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberNum(Str32 name) {
            if (Event != JsonEvent.ValueNumber || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        // ---
        public bool IsMemberStr(ref Str32 name) {
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberStr(Str32 name) {
            if (Event != JsonEvent.ValueString || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        // ---
        public bool IsMemberBln(ref Str32 name) {
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberBln(Str32 name) {
            if (Event != JsonEvent.ValueBool || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        // ---
        public bool IsMemberNul(ref Str32 name) {
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsMemberNul(Str32 name) {
            if (Event != JsonEvent.ValueNull || !key.IsEqual32(name))
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        // ---
        public void UseMember() {
            switch (Event) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                    nodeFlags[stateLevel] |= NodeFlags.Found;
                    break;
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    nodeFlags[stateLevel - 1] |= NodeFlags.Found;
                    break;
            }
        }

        // ----------- array element checks -----------
        public bool IsElementObj() {
            if (Event != JsonEvent.ObjectStart)
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementObj(Str32 name) {
            if (Event != JsonEvent.ObjectStart)
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementArr() {
            if (Event != JsonEvent.ArrayStart)
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementArr(Str32 name) {
            if (Event != JsonEvent.ArrayStart)
                return false;
            nodeFlags[stateLevel - 1] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementNum() {
            if (Event != JsonEvent.ValueNumber)
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementStr() {
            if (Event != JsonEvent.ValueString)
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementBln() {
            if (Event != JsonEvent.ValueBool)
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }
        
        public bool IsElementNul() {
            if (Event != JsonEvent.ValueNull)
                return false;
            nodeFlags[stateLevel] |= NodeFlags.Found;
            return true;
        }

        public void UseElement() {
            UseMember();
        }
    }
}