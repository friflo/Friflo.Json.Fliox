// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    // PropType
    public class PropType : IDisposable
    {
        public  readonly Type                       nativeType;
        public  readonly Bytes                      typeName;

        private readonly FFMap<String, PropField>   strMap      = new HashMapOpen<String, PropField>(13);
        private readonly FFMap<Bytes, PropField>    fieldMap    = new HashMapOpen<Bytes, PropField>(11);
        public  readonly PropertyFields             propFields;
        private readonly ConstructorInfo            constructor;
        
        
        public void Dispose() {
            typeName.Dispose();
            propFields.Dispose();
        }

        // PropType
        internal PropType (Type type, String name)
        {
            nativeType = type;
            typeName = new Bytes(name);
            propFields = new  PropertyFields (type, this, true, true);
            for (int n = 0; n < propFields.num; n++)
            {
                PropField   field = propFields.fields[n];
                strMap.Put(field.name, field);
                fieldMap.Put(field.nameBytes, field);
            }
            constructor = Reflect.GetDefaultConstructor (type);
        }
        
        public Object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (nativeType.IsValueType)
                    return Activator.CreateInstance(nativeType);
                throw new FrifloException("No default constructor available for: " + nativeType.Name);
            }
            return Reflect.CreateInstance(constructor);
        }

        public PropField GetField (String name)
        {
            return strMap.Get(name);
        }

        public PropField GetField (Bytes fieldName)
        {
            return fieldMap.Get(fieldName);
        }
        
        /// <summary>
        /// In contrast to <see cref="typeStore"/> this Cache is by intention not thread safe.
        /// It is created within a <see cref="JsonReader"/> and <see cref="JsonWriter"/> to access type information
        /// without locking if already cached.
        /// </summary>
        public class Cache
        {
            private readonly    HashMapLang <Type, PropType>        typeMap =   new HashMapLang <Type,  PropType >();
            private readonly    HashMapLang <Bytes, PropType>       nameMap =   new HashMapLang <Bytes, PropType >();
            private readonly    HashMapLang <Type, PropCollection>  colMap =    new HashMapLang <Type,  PropCollection >();
            private readonly    TypeStore                           typeStore;
            
            public Cache (TypeStore typeStore)
            {
                this.typeStore = typeStore;
            }
            
            internal PropCollection GetCollection (Type type)
            {
                PropCollection colType = colMap.Get(type);
                if (colType == null) {
                    colType = typeStore.GetCollection(type);
                    colMap.Put(type, colType);
                }
                return colType;
            }
            
            public PropType GetType (Type type)
            {
                PropType propType = typeMap.Get(type);
                if (propType == null) {
                    propType = typeStore.GetType(type, null);
                    typeMap.Put(type, propType);
                }
                return propType;
            }

            public PropType GetTypeByName(Bytes name)
            {
                PropType propType = nameMap.Get(name);
                if (propType == null)
                {
                    lock (typeStore.nameMap)
                    {
                        propType = typeStore.nameMap.Get(name);
                    }
                    nameMap.Put(propType.typeName, propType);
                }
                return propType;
            }

        }
    }   
}
