// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.MapIL.Obj;
using Friflo.Json.Mapper.Utils;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.MapIL.Val
{
    public class EnumILMapper<T> : TypeMapper<T>
    {
        private   readonly  Type                            underlyingEnumType;
        private   readonly  Dictionary<BytesString, long>   stringToIntegral = new Dictionary<BytesString, long>();
        private   readonly  Dictionary<long, BytesString>   integralToString = new Dictionary<long, BytesString>();
        
        public override string DataTypeName() { return "enum"; }
        
        public EnumILMapper(Type type) :
            base(typeof(T), Nullable.GetUnderlyingType(typeof(T)) != null, true)
        {
            Type enumType = isNullable ? underlyingType : type;
            underlyingEnumType = Enum.GetUnderlyingType(enumType);
            // ReSharper disable once PossibleNullReferenceException
            FieldInfo[] fields = enumType.GetFields();
            for (int n = 0; n < fields.Length; n++) {
                FieldInfo enumField = fields[n];
                if (enumField.FieldType.IsEnum) {
                    Enum    enumValue       = (Enum)enumField.GetValue(type);
                    string  enumName        = enumField.Name;
                    object  enumConst       = enumField.GetRawConstantValue();
                    long    enumIntegral    = TypeUtils.GetIntegralValue(enumConst, typeof(T));
                    var     name            = new BytesString(enumName);
                    stringToIntegral.Add    (name, enumIntegral);
                    integralToString.TryAdd (enumIntegral, name);
                }
            }
        }
        

        public override void Write(JsonWriter writer, T slot) {
            throw new NotImplementedException();
        }

        public override T Read(JsonReader reader, T slot, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = parser.value;
                if (!stringToIntegral.TryGetValue(reader.keyRef, out long enumValue))
                    return ReadUtils.ErrorIncompatible<T>(reader, "enum value. Value unknown", this, ref parser, out success);
                success = true;
                return TypeUtils.GetEnumValueFromIntegral<T>(enumValue, underlyingEnumType);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out success);
                if (!success)
                    return default;
                return TypeUtils.GetEnumValueFromIntegral<T>(integralValue, underlyingEnumType);
            }
            return ValueUtils.CheckElse(reader, this, out success);
        }

        public override void WriteValueIL(JsonWriter writer, ClassMirror mirror, int primPos, int objPos) {
            long? integralValue = mirror.LoadLongNull(primPos);
            if (integralValue == null) {
                WriteUtils.AppendNull(writer);
                return;
            }
            if (!integralToString.TryGetValue((long)integralValue, out BytesString enumName))
                throw new InvalidOperationException($"invalid integral enum value: {integralValue} for enum type: {typeof(T)}" );
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendBytes(ref enumName.value);
            writer.bytes.AppendChar('\"');
        }

        public override bool ReadValueIL(JsonReader reader, ClassMirror mirror, int primPos, int objPos) {
            bool success;
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = parser.value;
                if (!stringToIntegral.TryGetValue(reader.keyRef, out long enumValue))
                    return ReadUtils.ErrorIncompatible<bool>(reader, "enum value. Value unknown", this, ref parser, out success);
                mirror.StoreLongNull(primPos, enumValue);
                return true;
            }
            ValueUtils.CheckElse(reader, this, out success);
            return success;
        }
    }
}

#endif
