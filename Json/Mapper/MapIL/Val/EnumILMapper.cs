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
    public struct EnumString {
        public  BytesString name;
        public  object      value;
    }
    
    public struct EnumIntegral {
        public  long    integral;
        public  object  value;
    }
    
   
    public class EnumILMapper<T> : TypeMapper<T>
    {
        private   readonly  Type                                    underlyingEnumType;
        private   readonly  Dictionary<BytesString, EnumIntegral>   stringToIntegral = new Dictionary<BytesString,  EnumIntegral>();
        private   readonly  Dictionary<long,        EnumString>     integralToString = new Dictionary<long,         EnumString>();

        
        public override string DataTypeName() { return "enum"; }
        
        public EnumILMapper(StoreConfig config, Type type) :
            base(config, typeof(T), Nullable.GetUnderlyingType(typeof(T)) != null, true)
        {
            Type enumType = isNullable ? nullableUnderlyingType : type;
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
                    var enumIntegralValue = new EnumIntegral() {integral = enumIntegral, value = enumValue};
                    stringToIntegral.Add    (name, enumIntegralValue);
                    var enumString = new EnumString {name = name, value = enumValue};
                    integralToString.TryAdd (enumIntegral, enumString);
                }
            }
        }

        public override void Dispose() {
            base.Dispose();
            foreach (var entry in stringToIntegral) {
                entry.Key.value.Dispose();
            }
        }

        // ----------------------------------------- Write / Read ----------------------------------------- 
        public override void Write(ref Writer writer, T slot) {
            if (IsNull(ref slot)) {
                writer.AppendNull();
                return;
            }
            long integralValue = TypeUtils.GetIntegralFromEnumValue(slot, underlyingEnumType);
            if (!integralToString.TryGetValue(integralValue, out EnumString enumName))
                throw new InvalidOperationException($"invalid integral enum value: {integralValue} for enum type: {typeof(T)}" );
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendBytes(ref enumName.name.value);
            writer.bytes.AppendChar('\"');
        }

        public override T Read(ref Reader reader, T slot, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = parser.value;
                if (!stringToIntegral.TryGetValue(reader.keyRef, out EnumIntegral enumValue))
                    return reader.ErrorIncompatible<T>("enum value. Value unknown", this, out success);
                success = true;
                return (T)enumValue.value;
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out success);
                if (!success)
                    return default;
                if (integralToString.TryGetValue(integralValue, out EnumString enumValue)) {
                    success = true;
                    return (T)enumValue.value;
                }
                return reader.ErrorIncompatible<T>("enum value. Value unknown", this, out success);
            }
            return reader.HandleEvent(this, out success);
        }

        // ------------------------------------- WriteValueIL / ReadValueIL ------------------------------------- 
        
        public override bool IsValueNullIL(ClassMirror mirror, int primPos, int objPos) {
            return mirror.LoadLongNull(primPos) == null;
        }

        public override void WriteValueIL(ref Writer writer, ClassMirror mirror, int primPos, int objPos) {
            long? integralValue = mirror.LoadLongNull(primPos);
#if DEBUG
            if (integralValue == null)
                throw new InvalidOperationException("Expect non null enum. Type: " + typeof(T));
#endif
            if (!integralToString.TryGetValue((long)integralValue, out EnumString enumName))
                throw new InvalidOperationException($"invalid integral enum value: {integralValue} for enum type: {typeof(T)}" );
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendBytes(ref enumName.name.value);
            writer.bytes.AppendChar('\"');
        }

        public override bool ReadValueIL(ref Reader reader, ClassMirror mirror, int primPos, int objPos) {
            bool success;
            var ev = reader.parser.Event;
            if (reader.parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = reader.parser.value;
                if (!stringToIntegral.TryGetValue(reader.keyRef, out EnumIntegral enumValue))
                    return reader.ErrorIncompatible<bool>( "enum value. Value unknown", this, out success);
                mirror.StoreLongNull(primPos, enumValue.integral);
                return true;
            }
            if (ev == JsonEvent.ValueNull) {
                if (!isNullable)
                    return reader.ErrorIncompatible<bool>(DataTypeName(), this, out success);
                mirror.StorePrimitiveNull(primPos);
            }
            reader.HandleEvent(this, out success);
            return success;
        }
    }
}

#endif
