// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public static class ArrayUtils {
        
        public static void AddListItem (IList list, ref Var item, VarType varType, int index, int startLen, bool nullable) {
            if (index < startLen) {
                if (nullable) {
                    switch (varType) {
                        case VarType.Double:    ((List<double?>) list)[index]= item.Dbl;    return;
                        case VarType.Float:     ((List<float?>)  list)[index]= item.Flt;    return;
                        case VarType.Long:      ((List<long?>)   list)[index]= item.Lng;    return;
                        case VarType.Int:       ((List<int?>)    list)[index]= item.Int;    return;
                        case VarType.Short:     ((List<short?>)  list)[index]= item.Short;  return;
                        case VarType.Byte:      ((List<byte?>)   list)[index]= item.Byte;   return;
                        case VarType.Bool:      ((List<bool?>)   list)[index]= item.Bool;   return;
                        default:
                            throw new InvalidOperationException("varType not supported: " + varType);
                    }
                } else {
                    switch (varType) {
                        case VarType.Double:    ((List<double>) list)[index]= item.Dbl;    return;
                        case VarType.Float:     ((List<float>)  list)[index]= item.Flt;    return;
                        case VarType.Long:      ((List<long>)   list)[index]= item.Lng;    return;
                        case VarType.Int:       ((List<int>)    list)[index]= item.Int;    return;
                        case VarType.Short:     ((List<short>)  list)[index]= item.Short;  return;
                        case VarType.Byte:      ((List<byte>)   list)[index]= item.Byte;   return;
                        case VarType.Bool:      ((List<bool>)   list)[index]= item.Bool;   return;
                        default:
                            throw new InvalidOperationException("varType not supported: " + varType);
                    }
                }
            }

            if (nullable) {
                switch (varType) {
                    case VarType.Double:    ((List<double?>) list).Add(item.Dbl);    return;
                    case VarType.Float:     ((List<float?>)  list).Add(item.Flt);    return;
                    case VarType.Long:      ((List<long?>)   list).Add(item.Lng);    return;
                    case VarType.Int:       ((List<int?>)    list).Add(item.Int);    return;
                    case VarType.Short:     ((List<short?>)  list).Add(item.Short);  return;
                    case VarType.Byte:      ((List<byte?>)   list).Add(item.Byte);   return;
                    case VarType.Bool:      ((List<bool?>)   list).Add(item.Bool);   return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }  
            } else {
                switch (varType) {
                    case VarType.Double:    ((List<double>) list).Add(item.Dbl);    return;
                    case VarType.Float:     ((List<float>)  list).Add(item.Flt);    return;
                    case VarType.Long:      ((List<long>)   list).Add(item.Lng);    return;
                    case VarType.Int:       ((List<int>)    list).Add(item.Int);    return;
                    case VarType.Short:     ((List<short>)  list).Add(item.Short);  return;
                    case VarType.Byte:      ((List<byte>)   list).Add(item.Byte);   return;
                    case VarType.Bool:      ((List<bool>)   list).Add(item.Bool);   return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }    
            }
        }
        
        public static bool StartArray(JsonReader reader, ref Var slot, StubType stubType, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (stubType.isNullable) {
                        slot.Obj = null;
                        success = true;
                        return false;
                    }
                    reader.ErrorIncompatible("array", stubType, ref reader.parser);
                    success = false;
                    return false;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    reader.ErrorIncompatible("array", stubType, ref reader.parser);
                    return false;
            }
        }

    }
}