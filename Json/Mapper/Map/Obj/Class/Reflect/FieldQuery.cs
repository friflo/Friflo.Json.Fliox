// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj.Class.Reflect
{
    public interface IProperties
    {
        // void     SetProperties (Property prop) ; 
    }
    
    public class  FieldQuery
    {
        internal readonly   List<PropField>     fieldList = new List <PropField>();
        internal            int                 primCount;
        internal            int                 objCount;

        private static readonly     Type[] Types = new Type [] { typeof( FieldQuery ) };

        
        private void CreatePropField (Type type, String fieldName, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            PropertyInfo getter = ReflectUtils.GetPropertyGet(type, fieldName );
            if (getter != null) {
                Type propType = getter.PropertyType;
                Type ut         = Nullable.GetUnderlyingType(propType);
                bool isNullablePrimitive = propType.IsValueType && ut != null && ut.IsPrimitive;
                
                if (addMembers) {
                    PropertyInfo setter = ReflectUtils.GetPropertySet(type, fieldName);
                    PropField pf = propType.IsValueType || isNullablePrimitive
                        ? new PropField(fieldName, propType, null, getter, setter, primCount, -1)
                        : new PropField(fieldName, propType, null, getter, setter, -1, objCount);
                    fieldList.Add(pf);
                }
                IncrementILCounts(propType, isNullablePrimitive);
                return;
            }
            // create property from field
            FieldInfo field = ReflectUtils.GetField(type, fieldName );
            if (field != null) {
                Type fieldType = field.FieldType;
                Type ut         = Nullable.GetUnderlyingType(fieldType);
                bool isNullablePrimitive = fieldType.IsValueType && ut != null && ut.IsPrimitive;
                
                if (addMembers) {
                    PropField pf = fieldType.IsValueType || isNullablePrimitive
                        ? new PropField(fieldName, fieldType, field, null, null, primCount, -1)
                        : new PropField(fieldName, fieldType, field, null, null, -1, objCount);
                    fieldList. Add (pf);
                }
                IncrementILCounts(fieldType, isNullablePrimitive);
                return;
            }
            throw new InvalidOperationException("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);
        }

        private void IncrementILCounts(Type memberType, bool isNullablePrimitive) {
            if (memberType.IsPrimitive || isNullablePrimitive) {
                primCount++;
            } else if (memberType.IsValueType) {
                // struct itself must not be incremented only its members. Their position need to be counted 
                TraverseMembers(memberType, false);
            } else
                objCount++; // object
        }

        private static MethodInfo GetPropertiesDeclaration (Type type) {
            return ReflectUtils.GetMethodEx(type, "SetProperties", Types);
        }

        internal void SetProperties (Type type) {
            MethodInfo method = GetPropertiesDeclaration(type);
            if (method != null) {
                Object[] args = new Object[] { this };
                ReflectUtils.Invoke (method, null, args);
            } else {
                TraverseMembers(type, true);
            }
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
