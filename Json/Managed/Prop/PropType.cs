// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    // PropType
    public class PropType : NativeType
    {
        private readonly FFMap<String, PropField>   strMap      = new HashMapOpen<String, PropField>(13);
        private readonly FFMap<Bytes, PropField>    fieldMap    = new HashMapOpen<Bytes,  PropField>(11);
        public  readonly PropertyFields             propFields;
        private readonly ConstructorInfo            constructor;
        
        
        public override void Dispose() {
            base.Dispose();
            propFields.Dispose();
        }

        // PropType
        internal PropType (TypeResolver resolver, Type type, IJsonCodec jsonCodec) :
            base (type, jsonCodec)
        {
            propFields = new  PropertyFields (resolver, type, this, true, true);
            for (int n = 0; n < propFields.num; n++)
            {
                PropField   field = propFields.fields[n];
                if (strMap.Get(field.name) != null)
                    throw new InvalidOperationException();
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
            private readonly    HashMapLang <Type,  NativeType> typeMap =      new HashMapLang <Type,  NativeType >();
            //
            private readonly    HashMapLang <Bytes, NativeType> nameToType =   new HashMapLang <Bytes, NativeType >();
            private readonly    HashMapLang <Type, Bytes>       typeToName =   new HashMapLang <Type,  Bytes >();
            
            private readonly    TypeStore                       typeStore;
            public              int                             lookupCount;
            
            public Cache (TypeStore typeStore) {
                this.typeStore = typeStore;
            }
            
            public NativeType GetType (Type type) {
                lookupCount++;
                NativeType propType = typeMap.Get(type);
                if (propType == null) {
                    propType = typeStore.GetType(type);
                    typeMap.Put(type, propType);
                }
                return propType;
            }

            public NativeType GetTypeByName(ref Bytes name) {
                NativeType propType = nameToType.Get(name);
                if (propType == null) {
                    lock (typeStore) {
                        propType = typeStore.nameToType.Get(name);
                    }
                }
                if (propType == null)
                    throw new InvalidOperationException("No type registered with discriminator: " + name);
                return propType;
            }

            public void AppendDiscriminator(ref Bytes dst, NativeType type) {
                Bytes name = typeToName.Get(type.type);
                if (!name.buffer.IsCreated()) {
                    lock (typeStore) {
                        name = typeStore.typeToName.Get(type.type);
                    }
                }
                if (!name.buffer.IsCreated())
                    throw new InvalidOperationException("no discriminator registered for type: " + type.type.FullName);
                dst.AppendBytes(ref name);
            }
        }
    }   
}
