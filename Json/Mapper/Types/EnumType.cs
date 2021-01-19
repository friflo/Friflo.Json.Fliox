using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
    /// <summary>
    /// The mapping <see cref="enumToString"/> and <see cref="stringToEnum"/> is not bidirectional as this is the behaviour of C# enum's
    /// <code>
    /// public enum TestEnum {
    ///     Value1 = 11,
    ///     Value2 = 11, // duplicate constant value - C#/.NET maps these enum values to the first value using same constant
    /// }
    /// </code>
    /// </summary>
    public class EnumType : StubType
    {
        internal readonly Dictionary<BytesString, Enum> stringToEnum = new Dictionary<BytesString, Enum>();
        internal readonly Dictionary<Enum, BytesString> enumToString = new Dictionary<Enum, BytesString>();
        //
        internal readonly Dictionary<long, Enum>        integralToEnum = new Dictionary<long, Enum>();

        public EnumType(Type type, IJsonMapper map) :
            base(type, map, true, TypeCat.String)
        {
            FieldInfo[] fields = type.GetFields();
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
    }




}