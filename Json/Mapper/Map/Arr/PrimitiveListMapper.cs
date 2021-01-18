// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Arr
{

    public class DoubleListMapper : IJsonMapper
    {
        public static readonly DoubleListMapper Interface = new DoubleListMapper();

        public StubType CreateStubType(Type type) {
            return ArrayUtils.CreatePrimitiveList(type, typeof(double), this);
        }

        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            List<double> list = (List<double>) slot.Obj;
            writer.bytes.AppendChar('[');
            for (int n = 0; n < list.Count; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                writer.format.AppendDbl(ref writer.bytes, list[n]);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (!ArrayUtils.StartArray(reader, ref slot, stubType, out bool startSuccess))
                return startSuccess;
        
            List<double> list = (List<double>) slot.Obj;
            if (list == null)
                list = new List<double>(JsonReader.minLen);
            list.Clear();
            int len = list.Count;
            int index = 0;
            while (true) {
                if (reader.parser.NextEvent() == JsonEvent.ValueNumber) {
                    var value = reader.parser.ValueAsDouble(out bool success);
                    if (!success)
                        return reader.ValueParseError();
                    if (index < len)
                        list[index++] = value;
                    else
                        list.Add(value);
                } else 
                    return ArrayUtils.ListElse(reader, ref slot, stubType, list);
            }
        }
    }
    
}