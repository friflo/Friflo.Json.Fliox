// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj.Reflect
{
    public class  FieldQuery
    {
        internal readonly   List<PropField>     fieldList = new List <PropField>();
        internal            int                 primCount;
        internal            int                 objCount;
        private  readonly   TypeStore           typeStore;

        internal FieldQuery(TypeStore typeStore, Type type) {
            this.typeStore = typeStore;
            TraverseMembers(type, true);
        }

        private void CreatePropField (Type type, String fieldName, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            PropertyInfo    getter = ReflectUtils.GetPropertyGet(type, fieldName );
            PropertyInfo    setter = null;
            FieldInfo       field = null;
            Type            memberType;
            if (getter != null) {
                setter = ReflectUtils.GetPropertySet(type, fieldName);
                memberType    = getter.PropertyType;
            } else {
                field = ReflectUtils.GetField(type, fieldName);
                memberType   = field.FieldType;
            }
            if (memberType == null)
                throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);

            TypeMapper  mapper      = typeStore.GetTypeMapper(memberType);
            Type        ut          = Nullable.GetUnderlyingType(memberType);
            bool isNullablePrimitive = memberType.IsValueType && ut != null && ut.IsPrimitive;
            bool isNullableEnum      = memberType.IsValueType && ut != null && ut.IsEnum;
            
            if (addMembers) {
                PropField pf;
                if (mapper.isValueType || isNullablePrimitive || isNullableEnum)
                    pf = new PropField(fieldName, mapper, memberType, field, getter, setter, primCount, -1);
                else
                    pf = new PropField(fieldName, mapper, memberType, field, getter, setter, -1, objCount);
                fieldList.Add(pf);
            }
            
            if (memberType.IsPrimitive || isNullablePrimitive || memberType.IsEnum || isNullableEnum) {
                primCount++;
            } else if (mapper.isValueType) {
                // struct itself must not be incremented only its members. Their position need to be counted 
                TraverseMembers(mapper.type, false);
            } else
                objCount++; // object
        }

        private void TraverseMembers(Type type, bool addMembers) {
            Type nullableStruct = TypeUtils.GetNullableStruct(type);
            if (nullableStruct != null) {
                primCount++;  // require array element to represent if Nullable<struct> is null or set (1) 
                TraverseMembers(nullableStruct, addMembers);
                return;
            }
            PropertyInfo[] properties = ReflectUtils.GetProperties(type);
            for (int n = 0; n < properties.Length; n++) {
                var name = properties[n].Name;
                CreatePropField(type, name, addMembers);
            }

            FieldInfo[] field = ReflectUtils.GetFields(type);
            for (int n = 0; n < field.Length; n++) {
                var name = field[n].Name;
                CreatePropField(type, name, addMembers);
            }
        }
    }
}
