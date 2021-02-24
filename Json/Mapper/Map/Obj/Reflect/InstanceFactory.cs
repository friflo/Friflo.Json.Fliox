using System;
using System.Collections.Generic;

namespace Friflo.Json.Mapper.Map.Obj.Reflect
{
    internal class PolyType
    {
        internal PolyType(Type type, string name) {
            this.type = type;
            this.name = name;
        }
        internal readonly Type   type;
        internal readonly string name;
    }

    internal class InstanceFactory {  // todo internal
        internal readonly   string                          discriminator;
        private  readonly   Type                            instanceType;
        private  readonly   PolyType[]                      polyTypes;
        private             TypeMapper                      instanceMapper;
        private  readonly   Dictionary<string, TypeMapper>  polymorphMapper = new Dictionary<string, TypeMapper>();

        private InstanceFactory(string discriminator, Type instanceType, PolyType[] polyTypes) {
            this.discriminator = discriminator;
            this.instanceType = instanceType;
            this.polyTypes = polyTypes;
        }

        internal void InitFactory(TypeStore typeStore) {
            if (instanceType != null)
                instanceMapper = typeStore.GetTypeMapper(instanceType);
            
            foreach (var polyType in polyTypes) {
                var mapper = typeStore.GetTypeMapper(polyType.type);
                mapper.discriminant = polyType.name;
                polymorphMapper.Add(polyType.name, mapper);
            }
        }

        internal object CreateInstance() {
            return instanceMapper.CreateInstance();
        }
        
        internal object CreatePolymorph(string name) {
            if (!polymorphMapper.TryGetValue(name, out TypeMapper mapper))
                return null;
            return mapper.CreateInstance();
        }
        
        
        internal static InstanceFactory GetInstanceFactory(Type type) {
            Type            instanceType = null;
            string          discriminator = null;
            List<PolyType>  typeList = new List<PolyType>();
            foreach (var attr in type.CustomAttributes) {
                if (attr.AttributeType == typeof(PolymorphAttribute)) {
                    string name = null;
                    if (attr.NamedArguments != null) {
                        foreach (var args in attr.NamedArguments) {
                            if (args.MemberName == nameof(PolymorphAttribute.Discriminant)) {
                                if (args.TypedValue.Value != null)
                                    name = (string) args.TypedValue.Value;
                            }
                        }
                    }
                    var arg = attr.ConstructorArguments;
                    var polyType = (Type) arg[0].Value;
                    if (polyType == null)
                        throw new InvalidOperationException($"[Polymorph(null)] type must not be null on: {type}");
                    if (!type.IsAssignableFrom(polyType))
                        throw new InvalidOperationException($"[Polymorph({polyType.Name})] type must extend annotated type: {type}");
                    typeList.Add(new PolyType(polyType, name ?? polyType.Name));
                } else if (attr.AttributeType == typeof(InstanceAttribute)) {
                    var arg = attr.ConstructorArguments;
                    instanceType = (Type) arg[0].Value;
                    if (instanceType == null)
                        throw new InvalidOperationException($"[Instance(null)] type must not be null on: {type}");
                    if (!type.IsAssignableFrom(instanceType))
                        throw new InvalidOperationException($"[Instance({instanceType.Name})] type must extend annotated type: {type}");
                } else if (attr.AttributeType == typeof(DiscriminatorAttribute)) {
                    if (attr.NamedArguments != null) {
                        var arg = attr.ConstructorArguments;
                        discriminator = (string) arg[0].Value;
                    }
                }
            }
            if (discriminator != null && typeList.Count == 0)
                throw new InvalidOperationException($"specified [Discriminator] require at least one [Polymorph] attribute on: {type}");
            if (discriminator == null && typeList.Count > 0)
                throw new InvalidOperationException($"specified [Polymorph] attribute require [Discriminator] on: {type}");

            if (instanceType != null || typeList.Count > 0)
                return new InstanceFactory(discriminator, instanceType, typeList.ToArray());
            return null;
        }

    }
}