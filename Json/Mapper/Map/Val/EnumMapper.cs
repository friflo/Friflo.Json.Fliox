// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class EnumMapper : IJsonMapper
    {
        public static readonly EnumMapper Interface = new EnumMapper();
        
        public StubType CreateStubType(Type type) {
            if (!type.IsEnum)
                return null;
            return new EnumType (type, Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            EnumType enumType = (EnumType) stubType;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString("\"item1\"");
            writer.bytes.AppendChar('\"');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            EnumType enumType = (EnumType) stubType;
            if (reader.parser.Event == JsonEvent.ValueString) {
                slot.Obj = null;
                return true;
                // return reader.ErrorNull("Failed parsing DateTime. value: ", value.ToString());
            }
            return ValueUtils.CheckElse(reader, stubType);
        }
    }
}
