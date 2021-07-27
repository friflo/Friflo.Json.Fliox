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
        public   override   ICollection<TypeDef>            SeparateTypes   { get; }
        /// <summary>Contains only non <see cref="Nullable"/> Type's</summary>
        private  readonly   Dictionary<Type, NativeTypeDef> nativeTypes;
        
        public NativeTypeSchema (TypeStore typeStore, ICollection<Type> separateTypes = null) {
            var typeMappers = typeStore.GetTypeMappers();
            
            // Collect all types into containers to simplify further processing
            nativeTypes     = new Dictionary<Type, NativeTypeDef>(typeMappers.Count);
            var types       = new HashSet<TypeDef>               (typeMappers.Count);
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                var underMapper     = mapper.GetUnderlyingMapper();
                if (IsNullableMapper(underMapper, out var nonNullableType)) {
                    typeStore.GetTypeMapper(nonNullableType);
                }
                if (nativeTypes.ContainsKey(nonNullableType))
                    continue;
                var  typeDef = new NativeTypeDef(underMapper);
                nativeTypes.Add(nonNullableType, typeDef);
                types.      Add(typeDef);
            }
            // in case any Nullable<> was found - typeStore contain now also their non-nullable counterparts.
            typeMappers = typeStore.GetTypeMappers();
            
            var standardTypes = new NativeStandardTypes(nativeTypes);
            Types           = types;
            StandardTypes   = standardTypes;
            SeparateTypes   = GetTypes(separateTypes);

            // Set the Name and Namespace of all TypeDefs 
            foreach (var pair in nativeTypes) {
                NativeTypeDef typeDef = pair.Value;
                typeDef.Name       = typeDef.mapper.type.Name;
                typeDef.Namespace  = typeDef.mapper.type.Namespace;
            }
            standardTypes.SetStandardNames();

            // set the base type (base class or parent class) for all types. 
            foreach (var pair in nativeTypes) {
                NativeTypeDef   typeDef        = pair.Value;
                Type            baseType    = typeDef.native.BaseType;
                TypeMapper      mapper;
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
                        var fieldDef = new FieldDef (propField.jsonName, required, type, isArray, isDictionary);
                        typeDef.fields.Add(fieldDef);
                    }
                }
                
                // set the unionType if a class is a discriminated union
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory != null) {
                    var polyTypes = instanceFactory.polyTypes;
                    typeDef.unionType  = new UnionType {
                        discriminator   = instanceFactory.discriminator,
                        types           = new List<TypeDef>(polyTypes.Length)
                    };
                    foreach (var polyType in polyTypes) {
                        TypeDef element = nativeTypes[polyType.type];
                        typeDef.unionType.types.Add(element);
                    }
                }
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
        
        private ICollection<TypeDef> GetTypes(ICollection<Type> types) {
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