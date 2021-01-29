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

        private void Set(Type type, String name)
        {
            CreatePropField (type, name, name);
        }

        public void Set(Type type, String name, String field)
        {
            CreatePropField (type, name, field);
        }
        
        private void CreatePropField (Type type, String name, String fieldName) {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            PropertyInfo getter = ReflectUtils.GetPropertyGet(type, fieldName );
            if (getter != null)
            {
                PropertyInfo setter = ReflectUtils.GetPropertySet(type, fieldName );
                Type propType = getter.PropertyType;
                // is struct?
                if (useIL && propType.IsValueType && !propType.IsPrimitive) {
                    
                }
                PropField pf = propType.IsPrimitive
                    ? new PropField(name, propType, null, getter, setter, primCount++, -1)
                    : new PropField(name, propType, null, getter, setter, -1, objCount++);
                fieldList. Add (pf);
                return;
            }
            // create property from field
            FieldInfo field = ReflectUtils.GetField(type, fieldName );
            if (field != null) {
                Type fieldType = field.FieldType;
                // is struct?
                if (useIL && fieldType.IsValueType && !fieldType.IsPrimitive) {
                    
                }
                PropField pf = fieldType.IsPrimitive
                    ? new PropField(name, fieldType,     field, null, null, primCount++, -1)
                    : new PropField(name, fieldType,     field, null, null, -1, objCount++);
                fieldList. Add (pf);
                return;
            }
            throw new FrifloException ("Field '" + name + "' ('" + fieldName + "') not found in type " + type);
        }


        private static MethodInfo GetPropertiesDeclaration (Type type)
        {
            return ReflectUtils.GetMethodEx(type, "SetProperties", Types);
        }

        internal void SetProperties (Type type)
        {
            try
            {
                MethodInfo method = GetPropertiesDeclaration(type);
                if (method != null)
                {
                    Object[] args = new Object[] { this };
                    ReflectUtils.Invoke (method, null, args);
                }
                else
                {
                    PropertyInfo[] properties = ReflectUtils.GetProperties(type);
                    for (int n = 0; n < properties. Length; n++)
                        Set(type, properties[n]. Name);

                    FieldInfo[] field = ReflectUtils.GetFields(type);
                    for (int n = 0; n < field. Length; n++)
                        Set(type, field[n]. Name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("SetProperties() failed for type: " + type, e);
            }
        }
    }
}
