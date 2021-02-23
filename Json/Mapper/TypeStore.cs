// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

#if !UNITY_5_3_OR_NEWER
    [assembly: CLSCompliant(true)]
#endif

namespace Friflo.Json.Mapper
{
    /// <summary>
    /// An immutable configuration class for settings which are used by the lifetime of a <see cref="TypeStore"/>  
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class StoreConfig {
        public  readonly bool       useIL;

        public StoreConfig(TypeAccess typeAccess = TypeAccess.Reflection) {
            this.useIL = typeAccess == TypeAccess.IL;
        }
    }

    public enum TypeAccess {
        Reflection,
        IL
    }
    
    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="Reader"/> and <see cref="JsonWriter"/> instances.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class TypeStore : IDisposable
    {
        private     readonly    Dictionary <Type,  TypeMapper>  typeMap=        new Dictionary <Type,  TypeMapper >();
        
        private     readonly    List<TypeMapper>                newTypes =      new List<TypeMapper>();
        public      readonly    ITypeResolver                   typeResolver;
        public      readonly    StoreConfig                     config;
        

        public                  int                             typeCreationCount;
        public                  int                             storeLookupCount;

        public TypeStore() {
            typeResolver = new DefaultTypeResolver();
            config = new StoreConfig();
        }
        
        public TypeStore(ITypeResolver resolver, StoreConfig config) {
            this.typeResolver   = resolver ?? new DefaultTypeResolver();
            this.config         = config ?? new StoreConfig();
        }
            
        public void Dispose() {
            lock (this) {
                foreach (var mapper in typeMap.Values)
                    mapper.Dispose();
            }
        }

        internal TypeMapper GetTypeMapper (Type type)
        {
            lock (this)
            {
                TypeMapper mapper = GetOrCreateTypeMapper(type);

                while (newTypes.Count > 0) {
                    int lastPos = newTypes.Count - 1;
                    TypeMapper last = newTypes[lastPos];
                    newTypes.RemoveAt(lastPos);
                    // Deferred initialization of TypeMapper to allow circular type dependencies.
                    // So it supports type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
                    last.InitTypeMapper(this);
                }
                if (mapper != null)
                    return mapper;
                
                throw new NotSupportedException($"Type not supported: " + type);
            }
        }
        
        private TypeMapper GetOrCreateTypeMapper(Type type) {
            storeLookupCount++;
            if (typeMap.TryGetValue(type, out TypeMapper mapper))
                return mapper;
            
            typeCreationCount++;

            var typeMapperType = GetTypeMapperType(type);
            if (typeMapperType != null) {
                mapper = (TypeMapper)ReflectUtils.CreateInstance(typeMapperType);
            } else {
                mapper = typeResolver.CreateTypeMapper(config, type);
            }

            if (mapper == null)
                mapper = TypeNotSupportedMatcher.CreateTypeNotSupported(config, type, "Found no TypeMapper in TypeStore");

            
            typeMap.Add(type, mapper);
            newTypes.Add(mapper);
            return mapper;
        }
        
        private static Type GetTypeMapperType(Type type) {
            foreach (var attr in type.CustomAttributes) {
                if (attr.AttributeType == typeof(JsonTypeAttribute)) {
                    if (attr.NamedArguments != null) {
                        foreach (var arg in attr.NamedArguments) {
                            if (arg.MemberName == nameof(JsonTypeAttribute.TypeMapper)) {
                                if (arg.TypedValue.Value != null) {
                                    var typeMapper = arg.TypedValue.Value as Type;
                                    if (typeMapper != null && typeMapper.IsSubclassOf(typeof(TypeMapper)))
                                        return typeMapper;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class JsonTypeAttribute : Attribute {
        public Type     TypeMapper    { get; set; }
        public string   Discriminator { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PolymorphAttribute : Attribute {
        public PolymorphAttribute (Type instance) {}
    }
    
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class InstanceAttribute : Attribute {
        public InstanceAttribute (Type instance) {}
    }

    
    public class InstanceFactory {  // todo internal
        internal readonly   string                          discriminator;
        private  readonly   Type                            instanceType;
        private  readonly   Type[]                          polyTypes;
        private             TypeMapper                      instanceMapper;
        private  readonly   Dictionary<string, TypeMapper>  polymorphMapper = new Dictionary<string, TypeMapper>();

        internal InstanceFactory(string discriminator, Type instanceType, Type[] polyTypes) {
            this.discriminator = discriminator;
            this.instanceType = instanceType;
            this.polyTypes = polyTypes;
        }

        internal void InitFactory(TypeStore typeStore) {
            if (instanceType != null)
                instanceMapper = typeStore.GetTypeMapper(instanceType);
            
            foreach (var polyType in polyTypes) {
                var mapper = typeStore.GetTypeMapper(polyType);
                polymorphMapper.Add(polyType.Name, mapper);
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

    }
    
    
    /*
     public class InstanceFactory<T> : InstanceFactory {
        
        public InstanceFactory(string discriminator) {
            
        }
        public abstract T CreateInstance(string name);

        internal override object CreateObject(string name) {
            return CreateInstance(name);
        }
    } */
}