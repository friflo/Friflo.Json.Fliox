// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Native
{
    public class NativeTypeSchema : TypeSchema
    {
        public   override   ICollection<TypeDef>    Types       { get; }
        public   override   TypeDef                 Boolean     { get; }
        public   override   TypeDef                 String      { get; }
        public   override   TypeDef                 Unit8       { get; }
        public   override   TypeDef                 Int16       { get; }
        public   override   TypeDef                 Int32       { get; }
        public   override   TypeDef                 Int64       { get; }
        public   override   TypeDef                 Float       { get; }
        public   override   TypeDef                 Double      { get; }
        public   override   TypeDef                 BigInteger  { get; }
        public   override   TypeDef                 DateTime    { get; }
        public   override   TypeDef                 JsonValue   { get; }
        
        /// Contains only non nullable Type's
        private  readonly   Dictionary<Type, NativeType>    nativeMap;
        
        public NativeTypeSchema (TypeStore typeStore) {
            var typeMappers = new Dictionary<Type, TypeMapper>(typeStore.GetTypeMappers());
            nativeMap   = new Dictionary<Type,    NativeType>(typeMappers.Count);
            var map     = new Dictionary<TypeDef, TypeMapper>(typeMappers.Count);
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                var underMapper     = mapper.GetUnderlyingMapper();
                if (IsNullableMapper(underMapper, out var nonNullableType)) {
                    typeStore.GetTypeMapper(nonNullableType);
                }
                if (nativeMap.ContainsKey(nonNullableType))
                    continue;
                var  iTyp = new NativeType(underMapper);
                nativeMap.Add(nonNullableType, iTyp);
                map.      Add(iTyp, underMapper);
            }
            // in case a Nullable<> was found - typeStore contain now also their non-nullable counterparts.
            typeMappers =  new Dictionary<Type, TypeMapper>(typeStore.GetTypeMappers());
            
            Types = map.Keys;
            Boolean     = FindTypeDef(typeof(bool));
            String      = FindTypeDef(typeof(string));
            Unit8       = FindTypeDef(typeof(byte));
            Int16       = FindTypeDef(typeof(short));
            Int32       = FindTypeDef(typeof(int));
            Int64       = FindTypeDef(typeof(long));
            Float       = FindTypeDef(typeof(float));
            Double      = FindTypeDef(typeof(double));
            BigInteger  = FindTypeDef(typeof(BigInteger));
            DateTime    = FindTypeDef(typeof(DateTime));
            JsonValue   = FindTypeDef(typeof(JsonValue));
            
            foreach (var pair in nativeMap) {
                NativeType  type        = pair.Value;
                Type        baseType    = type.native.BaseType;
                TypeMapper  mapper;
                // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
                // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
                while (!typeMappers.TryGetValue(baseType, out  mapper)) {
                    baseType = baseType.BaseType;
                    if (baseType == null)
                        break;
                }
                if (mapper != null) {
                    type.baseType = nativeMap[mapper.type];
                }
            }
            foreach (var pair in nativeMap) {
                NativeType  type    = pair.Value;
                TypeMapper  mapper  = type.mapper;
                var elementMapper = mapper.GetElementMapper();
                if (elementMapper != null) {
                    elementMapper = elementMapper.GetUnderlyingMapper();
                    type.ElementType    = nativeMap[elementMapper.type];
                }
                var  propFields = mapper.propFields;
                if (propFields != null) {
                    type.fields = new List<Field>(propFields.fields.Length);
                    foreach (var propField in propFields.fields) {
                        var fieldMapper = propField.fieldType.GetUnderlyingMapper();
                        var isNullable = IsNullableMapper(fieldMapper, out var nonNullableType);
                        var field = new Field {
                            name        = propField.jsonName,
                            required    = propField.required || !isNullable,
                            type        = nativeMap[nonNullableType]
                        };
                        type.Fields.Add(field);
                    }
                }
                
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory != null) {
                    var polyTypes = instanceFactory.polyTypes;
                    type.unionType = new UnionType {
                        discriminator = instanceFactory.discriminator,
                        types = new List<TypeDef>(polyTypes.Length)
                    };
                    foreach (var polyType in polyTypes) {
                        TypeDef element = nativeMap[polyType.type];
                        type.unionType.types.Add(element);
                    }
                }
            }
        }
        
        private TypeDef FindTypeDef (Type type) {
            if (nativeMap.TryGetValue(type, out var typeDef))
                return typeDef;
            return null;
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
        
        public ICollection<TypeDef> GetTypes(ICollection<Type> nativeTypes) {
            if (nativeTypes == null)
                return null;
            var list = new List<TypeDef> (nativeTypes.Count);
            foreach (var nativeType in nativeTypes) {
                var type = nativeMap[nativeType];
                list.Add(type);
            }
            return list;
        }
    }
}