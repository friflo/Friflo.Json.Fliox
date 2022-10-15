// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

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
        public   readonly   string                          discriminator;
        public   readonly   string                          description;
        private  readonly   Type                            instanceType;
        public   readonly   PolyType[]                      polyTypes;
        private             TypeMapper                      instanceMapper;
        private  readonly   Dictionary<string, TypeMapper>  mapperByDiscriminator = new Dictionary<string, TypeMapper>();
        private  readonly   Dictionary<Type,   TypeMapper>  mapperByType          = new Dictionary<Type,   TypeMapper>();
        public   readonly   bool                            isAbstract;

        private InstanceFactory(string discriminator, string description, Type instanceType, PolyType[] polyTypes) {
            this.discriminator  = discriminator;
            this.description    = description;
            this.instanceType   = instanceType;
            this.polyTypes      = polyTypes;
        }
        
        public InstanceFactory() {
            isAbstract = true;
        }

        internal void InitFactory(TypeStore typeStore) {
            if (isAbstract)
                return;
            if (instanceType != null)
                instanceMapper = typeStore.GetTypeMapper(instanceType);
            
            foreach (var polyType in polyTypes) {
                var mapper = typeStore.GetTypeMapper(polyType.type);
                mapper.discriminant = polyType.name;
                mapperByDiscriminator.Add(polyType.name, mapper);
                mapperByType         .Add(polyType.type, mapper);
            }
        }

        internal object CreateInstance(Type type) {
            if (isAbstract)
                throw new InvalidOperationException($"type requires concrete types by [InstanceType()] or [PolymorphType()] on: {type.Name}");
            return instanceMapper.CreateInstance();
        }
        
        internal object CreatePolymorph(string name) {
            if (!mapperByDiscriminator.TryGetValue(name, out TypeMapper mapper))
                return null;
            return mapper.CreateInstance();
        }
        
        internal object CreatePolymorph(Type type) {
            if (!mapperByType.TryGetValue(type, out TypeMapper mapper))
                return null;
            return mapper.CreateInstance();
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