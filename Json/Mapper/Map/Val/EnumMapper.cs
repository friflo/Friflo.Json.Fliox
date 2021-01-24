// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Val
{
    public class EnumMatcher : ITypeMatcher {
        public static readonly EnumMatcher Instance = new EnumMatcher();
        
        public ITypeMapper CreateStubType(Type type) {
            if (!EnumMapper.IsEnum(type, out bool isNullable))
                return null;
            return new EnumMapper (type, isNullable);
        }
        
    }

    /// <summary>
    /// The mapping <see cref="enumToString"/> and <see cref="stringToEnum"/> is not bidirectional as this is the behaviour of C# enum's
    /// <code>
    /// public enum TestEnum {
    ///     Value1 = 11,
    ///     Value2 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant
    /// }
    /// </code>
    /// </summary>    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class EnumMapper : TypeMapper<Enum>
    {
        internal readonly Dictionary<BytesString, Enum> stringToEnum = new Dictionary<BytesString, Enum>();
        internal readonly Dictionary<Enum, BytesString> enumToString = new Dictionary<Enum, BytesString>();
        //
        internal readonly Dictionary<long, Enum>        integralToEnum = new Dictionary<long, Enum>();
        
        public override string DataTypeName() { return "enum"; }
        
        public EnumMapper(Type type, bool isNullable) :
            base (type, isNullable)
        {
            Type enumType = isNullable ? Nullable.GetUnderlyingType(type): type;
            // ReSharper disable once PossibleNullReferenceException
            FieldInfo[] fields = enumType.GetFields();
            for (int n = 0; n < fields.Length; n++) {
                FieldInfo enumField = fields[n];
                if (enumField.FieldType.IsEnum) {
                    Enum    enumValue       = (Enum)enumField.GetValue(type);
                    string  enumName        = enumField.Name;
                    object  enumConst       = enumField.GetRawConstantValue();
                    long    enumIntegral    = GetIntegralValue(enumConst);
                    var     name            = new BytesString(enumName);
                    stringToEnum.Add(name, enumValue);
                    enumToString.  TryAdd(enumValue, name);
                    integralToEnum.TryAdd(enumIntegral, enumValue);
                }
            }
            /*
            Type underlyingType = Enum.GetUnderlyingType(type);
            Array enumValues = Enum.GetValues(type);
            string[] enumNames = Enum.GetNames(type);

            for (int n = 0; n < enumValues.Length; n++) {
                Enum enumValue = (Enum)enumValues.GetValue(n);
                string enumName = enumNames[n];
                var name            = new BytesString(enumName);
                stringToEnum.TryAdd(name, enumValue);
                enumToString.Add(enumValue, name);
                // long underlyingValue = GetIntegralValue(enumValue, underlyingType);
                // integralToEnum.TryAdd(underlyingValue, enumValue);
            } */
        }

        public override void Dispose() {
            foreach (var key in stringToEnum.Keys)
                key.value.Dispose();
        }

        public override void InitStubType(TypeStore typeStore) {
        }

        private long GetIntegralValue(object enumConstant) {
            if (enumConstant is long    longVal)    return longVal;
            if (enumConstant is int     intVal)     return intVal;
            if (enumConstant is short   shortVal)   return shortVal;
            if (enumConstant is byte    byteVal)    return byteVal;
            if (enumConstant is uint    uintVal)    return uintVal;
            if (enumConstant is ushort  ushortVal)  return ushortVal;
            if (enumConstant is sbyte   sbyteVal)   return sbyteVal;

            throw new InvalidOperationException("UnderlyingType of Enum not supported. Enum: " + type);
        }
        
        public static bool IsEnum(Type type, out bool isNullable) {
            isNullable = false;
            if (!type.IsEnum) {
                Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( Nullable<>) );
                if (args == null)
                    return false;
                Type nullableType = args[0];
                if (!nullableType.IsEnum)
                    return false;
                isNullable = true;
            }
            return true;
        }

        public override void Write(JsonWriter writer, Enum slot) {
            if (enumToString.TryGetValue(slot, out BytesString enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(ref enumName.value);
                writer.bytes.AppendChar('\"');
            }
        }

        public override Enum Read(JsonReader reader, Enum slot, out bool success) {
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.keyRef.value = parser.value;
                if (stringToEnum.TryGetValue(reader.keyRef, out Enum enumValue)) {
                    success = true;
                    return enumValue;
                }
                return ReadUtils.ErrorIncompatible(reader, "enum value. Value unknown", this, ref parser, out success);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out success);
                if (!success)
                    return default;
                if (integralToEnum.TryGetValue(integralValue, out Enum enumValue)) {
                    success = true;
                    return enumValue;
                }
                return ReadUtils.ErrorIncompatible(reader, "enum value. Value unknown", this, ref parser, out success);
            }
            return ValueUtils.CheckElse(reader, this, out success);
        }
    }
}
