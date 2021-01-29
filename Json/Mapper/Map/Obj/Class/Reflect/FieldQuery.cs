// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
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
        private             bool                useIL;

        private static readonly     Type[] Types = new Type [] { typeof( FieldQuery ) };


        internal FieldQuery(bool useIL) {
            this.useIL = useIL;
        }

        private void CreatePropField (Type type, String fieldName, bool addMembers) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            PropertyInfo getter = ReflectUtils.GetPropertyGet(type, fieldName );
            if (getter != null) {
                Type propType = getter.PropertyType;
                if (addMembers) {
                    PropertyInfo setter = ReflectUtils.GetPropertySet(type, fieldName);
                    PropField pf = propType.IsValueType
                        ? new PropField(fieldName, propType, null, getter, setter, primCount, -1)
                        : new PropField(fieldName, propType, null, getter, setter, -1, objCount);
                    fieldList.Add(pf);
                }
                IncrementILCounts(propType);
                return;
            }
            // create property from field
            FieldInfo field = ReflectUtils.GetField(type, fieldName );
            if (field != null) {
                Type fieldType = field.FieldType;
                if (addMembers) {
                    PropField pf = fieldType.IsValueType
                        ? new PropField(fieldName, fieldType, field, null, null, primCount, -1)
                        : new PropField(fieldName, fieldType, field, null, null, -1, objCount);
                    fieldList. Add (pf);
                }
                IncrementILCounts(fieldType);
                return;
            }
            throw new FrifloException ("Field '" + fieldName + "' ('" + fieldName + "') not found in type " + type);
        }

        private void IncrementILCounts(Type memberType) {
            if (memberType.IsPrimitive)
                primCount++;
            else if (memberType.IsValueType)
                // struct itself must not be incremented only its members. Their position need to be counted 
                TraverseMembers(memberType, false);
            else
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
            PropertyInfo[] properties = ReflectUtils.GetProperties(type);
            for (int n = 0; n < properties. Length; n++)
                CreatePropField(type, properties[n]. Name, addMembers);

            FieldInfo[] field = ReflectUtils.GetFields(type);
            for (int n = 0; n < field. Length; n++)
                CreatePropField(type, field[n]. Name, addMembers);
        }
    }
}
