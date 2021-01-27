// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Class.Reflect
{
    // PropertyFields
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PropertyFields : Property, IDisposable
    {
        public      readonly    PropField []        fields;
        public      readonly    PropField []        fieldsSerializable;
        public      readonly    int                 num;
        
        private     readonly    Type                type;
        private     readonly    List<PropField>     fieldList = new List <PropField>();
        
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private     readonly    String              typeName;

        // ReSharper disable once UnusedMember.Local
        private static readonly Type[]                      Types = { typeof( PropCall ) };

        public PropertyFields (Type type)
        {
            this.type           = type;
            this.typeName       = type. ToString();
            try
            {
                SetProperties(type);
                num = fieldList. Count;
                fields = new PropField [num];
                for (int n = 0; n < num; n++)
                    fields[n] = fieldList [n];
                fieldList. Clear();
                // to array
                fieldsSerializable = new PropField [num];
                int pos = 0;
                for (int n = 0; n < num; n++)
                    fieldsSerializable[pos++] = fields[n];
            }
            catch (Exception e)
            {
                throw new FrifloException ("Failed reading properties from type: " + typeName, e);
            }
        }
        
        public void Dispose() {
            for (int i = 0; i < fields.Length; i++)
                fields[i].Dispose();
        }

        private void CreatePropField (String name, String fieldName)
        {
            // getter have higher priority than fields with the same fieldName. Same behavior as other serialization libs
            PropertyInfo getter = ReflectUtils.GetPropertyGet(type, fieldName );
            if (getter != null)
            {
                PropertyInfo setter = ReflectUtils.GetPropertySet(type, fieldName );
                PropField pf = new PropField(name, getter.PropertyType, null, getter, setter);
                fieldList. Add (pf);
                return;
            }
            // create property from field
            FieldInfo field = ReflectUtils.GetField(type, fieldName );
            if (field != null) {
                PropField pf = new PropField(name, field.FieldType,     field, null, null);
                fieldList. Add (pf);
                return;
            }
            throw new FrifloException ("Field '" + name + "' ('" + fieldName + "') not found in type " + type);
        }

        protected override void Set(String name)
        {
            CreatePropField (name, name);
        }

        public override void Set(String name, String field)
        {
            CreatePropField (name, field);
        }
    }
}
