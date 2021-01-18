using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
    public class EnumType : StubType
    {
        private readonly Dictionary<BytesString, Enum> stringToEnum = new Dictionary<BytesString, Enum>();
        
        public EnumType(Type type, IJsonMapper map) :
            base(type, map, true, TypeCat.String)
        {
            // FieldInfo[] enumItems = type.GetFields();
            Type underlyingType = Enum.GetUnderlyingType(type);
            Array enumValues = Enum.GetValues(type);

            for (int n = 0; n < enumValues.Length; n++) {
                var enumValue = enumValues.GetValue(n);
                object underlyingValue = Convert.ChangeType(enumValue, underlyingType);
            }

            var enumNames = Enum.GetNames(type);
            
            for (int n = 0; n < enumNames.Length; n++) {
                string enumName = enumNames[n];
            }

            /*
            FieldInfo[] enumItems = type.GetFields();
            for (int n = 0; n < enumItems.Length; n++) {
                var enumItem = enumItems[n];
                bool isLiteral =  enumItem.IsLiteral;
            } */
            
        }

        public override void InitStubType(TypeStore typeStore) {
        }
    }




}