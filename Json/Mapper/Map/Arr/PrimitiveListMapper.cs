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
    public static class PrimitiveList
    {
        public static readonly PrimitiveListMapper<double>   DoubleInterface =   new PrimitiveListMapper<double>  ();
        public static readonly PrimitiveListMapper<float>    FloatInterface =    new PrimitiveListMapper<float>   ();
        public static readonly PrimitiveListMapper<long>     LongInterface =     new PrimitiveListMapper<long>    ();
        public static readonly PrimitiveListMapper<int>      IntInterface =      new PrimitiveListMapper<int>     ();
        public static readonly PrimitiveListMapper<short>    ShortInterface =    new PrimitiveListMapper<short>   ();
        public static readonly PrimitiveListMapper<byte>     ByteInterface =     new PrimitiveListMapper<byte>    ();
        public static readonly PrimitiveListMapper<bool>     BoolInterface =     new PrimitiveListMapper<bool>    ();
        //
        public static readonly PrimitiveListMapper<double?>   DoubleNulInterface =   new PrimitiveListMapper<double?>  ();
        public static readonly PrimitiveListMapper<float?>    FloatNulInterface =    new PrimitiveListMapper<float?>   ();
        public static readonly PrimitiveListMapper<long?>     LongNulInterface =     new PrimitiveListMapper<long?>    ();
        public static readonly PrimitiveListMapper<int?>      IntNulInterface =      new PrimitiveListMapper<int?>     ();
        public static readonly PrimitiveListMapper<short?>    ShortNulInterface =    new PrimitiveListMapper<short?>   ();
        public static readonly PrimitiveListMapper<byte?>     ByteNulInterface =     new PrimitiveListMapper<byte?>    ();
        public static readonly PrimitiveListMapper<bool?>     BoolNulInterface =     new PrimitiveListMapper<bool?>    ();
        
        public static void AddListItemNull (IList list, int index, int startLen) {
            if (index < startLen)
                list[index] = null;
            else
                list.Add(null);
        }
        
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

        public static void GetListItem (IList list, ref Var item, VarType varType, int index, bool nullable) {
            if (nullable) {
                switch (varType) {
                    case VarType.Double:    item.NulDbl   = ((List<double?>) list)[index];  return;
                    case VarType.Float:     item.NulFlt   = ((List<float?>)  list)[index];  return;
                    case VarType.Long:      item.NulLng   = ((List<long?>)   list)[index];  return;
                    case VarType.Int:       item.NulInt   = ((List<int?>)    list)[index];  return;
                    case VarType.Short:     item.NulShort = ((List<short?>)  list)[index];  return;
                    case VarType.Byte:      item.NulByte  = ((List<byte?>)   list)[index];  return;
                    case VarType.Bool:      item.NulBool  = ((List<bool?>)   list)[index];  return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            } else {
                switch (varType) {
                    case VarType.Double:    item.Dbl =   ((List<double>) list)[index];   return;
                    case VarType.Float:     item.Flt =   ((List<float>)  list)[index];   return;
                    case VarType.Long:      item.Lng =   ((List<long>)   list)[index];   return;
                    case VarType.Int:       item.Int =   ((List<int>)    list)[index];   return;
                    case VarType.Short:     item.Short = ((List<short>)  list)[index];   return;
                    case VarType.Byte:      item.Byte =  ((List<byte>)   list)[index];   return;
                    case VarType.Bool:      item.Bool =  ((List<bool>)   list)[index];   return;
                    default:
                        throw new InvalidOperationException("varType not supported: " + varType);
                }
            }
        }
    }
    
    public class PrimitiveListMapper<T> : IJsonMapper
    {
        private readonly Type       elemType;
        private readonly VarType    elemVarType;
        
        public PrimitiveListMapper () {
            elemType            = typeof(T);
            elemVarType         = Var.GetVarType(elemType);
        }
        
        public StubType CreateStubType(Type type) {
            if (StubType.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                if (elemVarType == VarType.Object)
                    return null;
                if (elemType != elementType)
                    return null;
                
                ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                return new CollectionType  (type, elementType, this, 1, null, constructor);
            }
            return null;
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            List<T> list = (List<T>) slot.Obj;
            CollectionType collectionType = (CollectionType) stubType;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var();
            for (int n = 0; n < list.Count; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                PrimitiveList.GetListItem(list, ref elemVar, elementType.varType, n, elementType.isNullable);
                elementType.map.Write(writer, ref elemVar, elementType);
            }
            writer.bytes.AppendChar(']');
        }
        

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
            
            ref var parser = ref reader.parser;
            CollectionType collectionType = (CollectionType) stubType;
            List<T> list = (List<T>) slot.Obj;
            if (list == null)
                list = (List<T>) collectionType.CreateInstance();
            StubType elementType = collectionType.ElementType;
            bool nullable = elementType.isNullable;

            int startLen = list.Count;
            int index = 0;
            Var elemVar = new Var();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (elementType.typeCat != TypeCat.String)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        PrimitiveList.AddListItem(list, ref elemVar, elemVarType, index++, startLen, nullable);
                        break;
                    case JsonEvent.ValueNumber:
                        if (elementType.typeCat != TypeCat.Number)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        PrimitiveList.AddListItem(list, ref elemVar, elemVarType, index++, startLen, nullable);
                        break;
                    case JsonEvent.ValueBool:
                        if (elementType.typeCat != TypeCat.Bool)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        PrimitiveList.AddListItem(list, ref elemVar, elemVarType, index++, startLen, nullable);
                        break;
                    case JsonEvent.ValueNull:
                        if (!elementType.isNullable)
                            return reader.ErrorIncompatible("List element", elementType, ref parser);
                        PrimitiveList.AddListItemNull(list, index++, startLen);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        elemVar.Clear();
                        if (!elementType.map.Read(reader, ref elemVar, elementType))
                            return false;
                        PrimitiveList.AddListItem(list, ref elemVar, elemVarType, index++, startLen, nullable);
                        break;
                    case JsonEvent.ArrayEnd:
                        if (startLen - index > 0)
                            list.RemoveRange(index, startLen - index);
                        slot.Obj = list;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}