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

        private void CreatePropField (Type type, String fieldName, PropertyInfo property, FieldInfo field, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            Type            memberType;
            if (property != null) {
                memberType   = property.PropertyType;
            } else {
                memberType   = field.FieldType;
            }
            if (memberType == null)
                throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);

            TypeMapper  mapper      = typeStore.GetTypeMapper(memberType);
            Type        ut          = mapper.nullableUnderlyingType;
            bool isNullablePrimitive = ut != null && ut.IsPrimitive;
            bool isNullableEnum      = ut != null && ut.IsEnum;
            
            if (addMembers) {
                PropField pf;
                if (memberType.IsEnum || memberType.IsPrimitive || isNullablePrimitive || isNullableEnum) {
                    pf =     new PropField(fieldName, mapper, field, property, primCount,    -9999); // force index exception in case of buggy impl.
                } else {
                    if (mapper.isValueType)
                        pf = new PropField(fieldName, mapper, field, property, primCount, objCount);
                    else
                        pf = new PropField(fieldName, mapper, field, property, -9999,     objCount); // force index exception in case of buggy impl.
                }

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
                type = nullableStruct;
                primCount++;  // require array element to represent if Nullable<struct> is null or set (1) 
            }
            PropertyInfo[] properties = ReflectUtils.GetProperties(type);
            for (int n = 0; n < properties.Length; n++) {
                var name = properties[n].Name;
                CreatePropField(type, name, properties[n], null, addMembers);
            }

            FieldInfo[] field = ReflectUtils.GetFields(type);
            for (int n = 0; n < field.Length; n++) {
                var name = field[n].Name;
                CreatePropField(type, name, null, field[n], addMembers);
            }
        }
    }
}
