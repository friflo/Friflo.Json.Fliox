using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
    public class EnumType : StubType
    {
        internal readonly Dictionary<BytesString, Enum> stringToEnum = new Dictionary<BytesString, Enum>();
        internal readonly Dictionary<Enum, BytesString> enumToString = new Dictionary<Enum, BytesString>();
        
        public EnumType(Type type, IJsonMapper map) :
            base(type, map, true, TypeCat.String)
        {
            // FieldInfo[] enumItems = type.GetFields();
            Type underlyingType = Enum.GetUnderlyingType(type);
            Array enumValues = Enum.GetValues(type);
            string[] enumNames = Enum.GetNames(type);

            for (int n = 0; n < enumValues.Length; n++) {
                Enum enumValue = (Enum)enumValues.GetValue(n);
                string enumName = enumNames[n];
                var name = new BytesString(enumName);
                stringToEnum.Add(name, enumValue);
                enumToString.Add(enumValue, name);
                // object underlyingValue = Convert.ChangeType(enumValue, underlyingType);
            }
        }

        public override void Dispose() {
            foreach (var key in stringToEnum.Keys)
                key.value.Dispose();
        }

        public override void InitStubType(TypeStore typeStore) {
        }
    }




}