// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

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
            if (enumType.enumToString.TryGetValue((Enum)slot.Obj, out BytesString enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(ref enumName.value);
                writer.bytes.AppendChar('\"');
            }
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            EnumType enumType = (EnumType) stubType;
            if (reader.parser.Event == JsonEvent.ValueString) {
                reader.keyBuf.value = reader.parser.value;
                if (enumType.stringToEnum.TryGetValue(reader.keyBuf, out Enum enumValue)) {
                    slot.Obj = enumValue;
                    return true;
                }
                slot.Obj = null;
                return false;
                // return reader.ErrorNull("Failed parsing DateTime. value: ", value.ToString());
            }
            return ValueUtils.CheckElse(reader, stubType);
        }
    }
}
