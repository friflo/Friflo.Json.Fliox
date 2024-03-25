// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Pools;

namespace Friflo.Json.Fliox.Mapper.Map.Object.Reflect
{
    public sealed class PolyType
    {
        public   readonly   Type    type;
        public   readonly   string  name;
        
        public override     string  ToString() => $"{name} - {type.Name}";
        
        internal PolyType(Type type, string name) {
            this.type = type;
            this.name = name;
        }
    }

    // Is public to support external schema / code generators
    public sealed class InstanceFactory {
        public   readonly   string                              discriminator;
        public   readonly   string                              description;
        private  readonly   Type                                instanceType;
        public   readonly   PolyType[]                          polyTypes;
        internal readonly   byte[]                              discriminatorBytes;
        private             TypeMapper                          instanceMapper;
        private             Entry[]                             mappers;
        private  readonly   Dictionary<BytesHash, TypeMapper>   mapperByName;
        public   readonly   bool                                isAbstract;
        
        public              TypeMapper                          InstanceMapper => instanceMapper;
        
        readonly struct Entry
        {
            private  readonly JsonValue     name;
            internal readonly Type          type;
            internal readonly TypeMapper    mapper;

            public   override string        ToString() => $"{name}: {mapper.DataTypeName()}";

            internal Entry(in JsonValue name, Type type, TypeMapper mapper) {
                this.name   = name;
                this.type   = type;
                this.mapper = mapper;
            }
        }

        private InstanceFactory(string discriminator, string description, Type instanceType, PolyType[] polyTypes) {
            this.discriminator      = discriminator;
            this.discriminatorBytes = discriminator == null ? null : Encoding.UTF8.GetBytes(discriminator);
            this.description        = description;
            this.instanceType       = instanceType;
            this.polyTypes          = polyTypes;
            this.mapperByName       = new Dictionary<BytesHash, TypeMapper>(BytesHash.Equality);
        }
        
        public InstanceFactory() {
            isAbstract = true;
        }

        internal void InitFactory(TypeStore typeStore) {
            if (isAbstract)
                return;
            if (instanceType != null)
                instanceMapper = typeStore.GetTypeMapper(instanceType);
            
            var buffer = new Utf8Buffer();
            foreach (var polyType in polyTypes) {
                buffer.Add(polyType.name);
            }
            var names = buffer.AsBytes();
            mappers = new Entry[polyTypes.Length];
            for(int n = 0; n < polyTypes.Length; n++) {
                var polyType    = polyTypes[n];
                var mapper      = typeStore.GetTypeMapper(polyType.type);
                var name        = new BytesHash(names[n]); 
                mapper.discriminant = polyType.name;
                var jsonName    = new JsonValue(name.value);
                mappers[n]      = new Entry (jsonName, polyType.type, mapper);
                mapperByName.Add(name, mapper);
            }
        }

        internal object CreateInstance(ReaderPool readerPool, Type type) {
            if (isAbstract)
                throw new InvalidOperationException($"type requires concrete types by [InstanceType()] or [PolymorphType()] on: {type.Name}");
            if (readerPool == null)
                return instanceMapper.NewInstance();
            return readerPool.CreateObject(instanceMapper);
        }
        
        internal object CreatePolymorph(ReaderPool readerPool, in Bytes name, object obj, out TypeMapper mapper) {
            var key = new BytesHash (name);
            if (!mapperByName.TryGetValue(key, out mapper))
                return null;
            if (obj != null) {
                var objType = obj.GetType();
                if (objType == mapper.type)
                    return obj;
            }
            if (readerPool == null)
                return mapper.NewInstance();
            return readerPool.CreateObject(mapper);
        }
        
        internal object CreatePolymorph(Type type) {
            foreach (var mapper in mappers) {
                if (type != mapper.type)
                    continue;
                return mapper.mapper.NewInstance();    
            }
            return null;
        }
        
        
        internal static InstanceFactory GetInstanceFactory(Type type) {
            Type            instanceType    = null;
            string          discriminator   = null;
            string          discDescription = null;
            List<PolyType>  typeList        = new List<PolyType>();
            foreach (var attr in type.CustomAttributes) {
                if (attr.AttributeType == typeof(PolymorphTypeAttribute)) {
                    var     arg         = attr.ConstructorArguments;
                    var     polyType    = (Type) arg[0].Value;
                    string  name        = arg.Count < 2 ? null : (string)arg[1].Value;
                    if (polyType == null)
                        throw new InvalidOperationException($"[PolymorphType(null)] type must not be null on: {type.Name}");
                    if (!type.IsAssignableFrom(polyType))
                        throw new InvalidOperationException($"[PolymorphType({polyType.Name})] type must extend annotated type: {type.Name}");
                    typeList.Add(new PolyType(polyType, name ?? polyType.Name));
                } else if (attr.AttributeType == typeof(InstanceTypeAttribute)) {
                    var arg = attr.ConstructorArguments;
                    instanceType = (Type) arg[0].Value;
                    if (instanceType == null)
                        throw new InvalidOperationException($"[InstanceType(null)] type must not be null on: {type.Name}");
                    if (!type.IsAssignableFrom(instanceType))
                        throw new InvalidOperationException($"[InstanceType({instanceType.Name})] type must extend annotated type: {type.Name}");
                } else if (attr.AttributeType == typeof(DiscriminatorAttribute)) {
                    var arguments   = attr.ConstructorArguments;
                    discriminator   = (string) arguments[0].Value;
                    discDescription = arguments.Count < 2 ? null : (string) arguments[1].Value;
                }
            }
            if (discriminator != null && typeList.Count == 0)
                throw new InvalidOperationException($"specified [Discriminator] require at least one [PolymorphType] attribute on: {type.Name}");
            if (discriminator == null && typeList.Count > 0)
                throw new InvalidOperationException($"specified [PolymorphType] attribute require [Discriminator] on: {type.Name}");

            if (instanceType != null || typeList.Count > 0)
                return new InstanceFactory(discriminator, discDescription, instanceType, typeList.ToArray());
            if (type.IsAbstract)
                return new InstanceFactory();
            return null;
        }

    }
}