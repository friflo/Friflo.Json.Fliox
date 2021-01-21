// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ArrayMapper : ITypeMapper
    {
        public static readonly ArrayMapper Interface = new ArrayMapper();

        public string DataTypeName() { return "array"; }
        
        public StubType CreateStubType(Type type) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (Reflect.IsAssignableFrom(typeof(Object), elementType)) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, this, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            CollectionType collectionType = (CollectionType) stubType;
            Array arr = (Array) slot.Obj;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            Var elemVar = new Var();
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                // elemVar.Set(arr.GetValue(n), elementType.varType, elementType.isNullable);
                elemVar.Obj = arr.GetValue(n);
                if (elemVar.IsNull)
                    WriteUtils.AppendNull(writer);
                else
                    elementType.map.Write(writer, ref elemVar, elementType);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool success))
                return success;
            
            ref var parser = ref reader.parser;
            var collection = (CollectionType) stubType;
            int startLen;
            int len;
            Array array;
            if (slot.Obj == null) {
                startLen = 0;
                len = ReadUtils.minLen;
                array = Arrays.CreateInstance(collection.ElementType.type, len);
            }
            else {
                array = (Array) slot.Obj;
                startLen = len = array.Length;
            }

            StubType elementType = collection.ElementType;
            int index = 0;
            Var elemVar = new Var();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled via primitive array codecs
                        return ReadUtils.ErrorIncompatible(reader, "array element", elementType, ref parser);
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, len = ReadUtils.Inc(len));
                        if (!elementType.isNullable)
                            return ReadUtils.ErrorIncompatible(reader, "array element", elementType, ref parser);
                        array.SetValue(null, index++);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemVar.Obj = array.GetValue(index);
                            if(!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            array.SetValue(elemVar.Obj, index);
                        } else {
                            elemVar.SetObjNull();
                            if (!elementType.map.Read(reader, ref elemVar, elementType))
                                return false;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.ElementType.type, array, len = ReadUtils.Inc(len));
                            array.SetValue(elemVar.Obj, index);
                        }
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, index);
                        slot.Obj = array;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return ReadUtils.ErrorMsg(reader, "unexpected state: ", ev);
                }
            }
        }
    }
}
