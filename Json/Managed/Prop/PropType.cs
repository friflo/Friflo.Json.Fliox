// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    // PropType
    public class PropType : NativeType
    {
        public  readonly Bytes                      typeName;
        private readonly FFMap<String, PropField>   strMap      = new HashMapOpen<String, PropField>(13);
        private readonly FFMap<Bytes, PropField>    fieldMap    = new HashMapOpen<Bytes, PropField>(11);
        public  readonly PropertyFields             propFields;
        private readonly ConstructorInfo            constructor;
        
        
        public override void Dispose() {
            base.Dispose();
            typeName.Dispose();
            propFields.Dispose();
        }

        // PropType
        internal PropType (Type type, String name) :
            base (type, JsonReader.ReadObject)
        {
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
        
        public override Object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new FrifloException("No default constructor available for: " + type.Name);
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
            private readonly    HashMapLang <Type, NativeType>        typeMap =   new HashMapLang <Type,  NativeType >();
            private readonly    HashMapLang <Bytes, NativeType>       nameMap =   new HashMapLang <Bytes, NativeType >();
            
            private readonly    TypeStore                           typeStore;
            
            public Cache (TypeStore typeStore)
            {
                this.typeStore = typeStore;
            }
            
            public NativeType GetType (Type type)
            {
                NativeType propType = typeMap.Get(type);
                if (propType == null) {
                    propType = typeStore.GetType(type, null);
                    typeMap.Put(type, propType);
                }
                return propType;
            }

            public NativeType GetTypeByName(Bytes name)
            {
                NativeType propType = nameMap.Get(name);
                if (propType == null)
                {
                    lock (typeStore.nameMap)
                    {
                        propType = typeStore.nameMap.Get(name);
                        if (propType is PropType)
                            nameMap.Put(((PropType)propType).typeName, propType);
                    }
                }
                return propType;
            }

        }
    }   
}
