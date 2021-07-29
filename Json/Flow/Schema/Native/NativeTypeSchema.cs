// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Native
{
    public class NativeTypeSchema : TypeSchema
    {
        public   override   ICollection<TypeDef>            Types           { get; }
        public   override   StandardTypes                   StandardTypes   { get; }

        /// <summary>Contains only non <see cref="Nullable"/> Type's</summary>
        private  readonly   Dictionary<Type, NativeTypeDef> nativeTypes;
        
        public NativeTypeSchema (TypeStore typeStore) {
            var typeMappers = typeStore.GetTypeMappers();
            
            // Collect all types into containers to simplify further processing
            nativeTypes     = new Dictionary<Type, NativeTypeDef>(typeMappers.Count);
            var types       = new List<TypeDef>                  (typeMappers.Count);
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                AddType(types, mapper, typeStore);
            }
            // in case any Nullable<> was found - typeStore contain now also their non-nullable counterparts.
            typeMappers = typeStore.GetTypeMappers();
            
            var standardTypes = new NativeStandardTypes(nativeTypes);
            Types           = types;
            StandardTypes   = standardTypes;

            // set the base type (base class or parent class) for all types. 
            foreach (var pair in nativeTypes) {
                NativeTypeDef   typeDef        = pair.Value;
                Type            baseType    = typeDef.native.BaseType;
                TypeMapper      mapper = null;
                // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
                // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
                while (!typeMappers.TryGetValue(baseType, out  mapper)) {
                    baseType = baseType.BaseType;
                    if (baseType == null)
                        break;
                }
                if (mapper != null) {
                    typeDef.baseType = nativeTypes[mapper.type];
                }
            }
            foreach (var pair in nativeTypes) {
                NativeTypeDef   typeDef = pair.Value;
                TypeMapper      mapper  = typeDef.mapper;
                
                // set the fields for classes or structs
                var  propFields = mapper.propFields;
                if (propFields != null) {
                    typeDef.fields = new List<FieldDef>(propFields.fields.Length);
                    foreach (var propField in propFields.fields) {
                        var fieldMapper     = propField.fieldType.GetUnderlyingMapper();
                        var isNullable      = IsNullableMapper(fieldMapper, out var nonNullableType);
                        var isArray         = fieldMapper.IsArray;
                        var isDictionary    = fieldMapper.type.GetInterfaces().Contains(typeof(IDictionary));
                        NativeTypeDef type;
                        if (isArray || isDictionary) {
                            var elementMapper = fieldMapper.GetElementMapper().GetUnderlyingMapper();
                            type = nativeTypes[elementMapper.type];
                        } else {
                            type            = nativeTypes[nonNullableType];
                        }
                        var required = propField.required || !isNullable;
                        var fieldDef = new FieldDef (propField.jsonName, required, type, isArray, isDictionary, typeDef);
                        typeDef.fields.Add(fieldDef);
                    }
                }
                if (typeDef.Discriminant != null) {
                    var baseType = typeDef.baseType;
                    while (baseType != null) {
                        var unionType = baseType.unionType;
                        if (unionType != null) {
                            typeDef.discriminator = unionType.discriminator;
                            break;
                        }
                        baseType = baseType.baseType;
                    }
                    if (typeDef.discriminator == null)
                        throw new InvalidOperationException($"found no discriminator in base classes. type: {typeDef}");
                }
                // set the unionType if a class is a discriminated union
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory != null && !instanceFactory.isAbstract) {
                    var polyTypes   = instanceFactory.polyTypes;
                    var unionTypes  = new List<TypeDef>(polyTypes.Length);
                    foreach (var polyType in polyTypes) {
                        TypeDef element = nativeTypes[polyType.type];
                        unionTypes.Add(element);
                    }
                    typeDef.unionType  = new UnionType (instanceFactory.discriminator, unionTypes);
                    typeDef.isAbstract = true;
                }
            }
        }
        
        private void AddType(List<TypeDef> types, TypeMapper  mapper, TypeStore typeStore) {
            mapper  = mapper.GetUnderlyingMapper();
            if (IsNullableMapper(mapper, out var nonNullableType)) {
                typeStore.GetTypeMapper(nonNullableType);
            }
            if (nativeTypes.ContainsKey(nonNullableType))
                return;
            NativeTypeDef typeDef;
            if (NativeStandardTypes.Types.TryGetValue(nonNullableType, out string name)) {
                typeDef = new NativeTypeDef(mapper, name, "Standard");
            } else {
                typeDef = new NativeTypeDef(mapper, mapper.type.Name, mapper.type.Namespace);
            }
            nativeTypes.Add(nonNullableType, typeDef);
            types.      Add(typeDef);
            var baseType = nonNullableType.BaseType;
            if (baseType != null && baseType != typeof(object) && baseType != typeof(Enum) && baseType != typeof(ValueType)) {
                var baseMapper = typeStore.GetTypeMapper(baseType);
                AddType(types, baseMapper, typeStore);
            }
        }
        
        private static bool IsNullableMapper(TypeMapper mapper, out Type nonNullableType) {
            var isNullable = mapper.isNullable;
            if (isNullable && mapper.nullableUnderlyingType != null) {
                nonNullableType = mapper.nullableUnderlyingType;
                return true;
            }
            nonNullableType = mapper.type;
            return isNullable;
        }
        
        public ICollection<TypeDef> TypesAsTypeDefs(ICollection<Type> types) {
            if (types == null)
                return null;
            var list = new List<TypeDef> (types.Count);
            foreach (var nativeType in types) {
                var type = nativeTypes[nativeType];
                list.Add(type);
            }
            return list;
        }
    }
}