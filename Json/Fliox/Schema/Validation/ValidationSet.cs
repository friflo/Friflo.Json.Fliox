// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Schema.Validation
{
    /// <summary>
    /// <see cref="ValidationSet"/> provide the validation rules for <see cref="TypeValidator"/> to validate
    /// arbitrary JSON payloads by <see cref="TypeValidator.ValidateObject"/>.
    /// </summary>
    public sealed class ValidationSet
    {
        private  readonly   List<ValidationTypeDef>                 types;      // todo rename -> typeDefs
        private  readonly   Dictionary<TypeDef, ValidationTypeDef>  typeMap;    // todo rename -> typeDefMap
        
        internal            IEnumerable<ValidationTypeDef>          TypeDefs => types;

        public ValidationType GetValidationType (TypeDef typeDef) {
            if (typeMap.TryGetValue(typeDef, out var validationTypeDef))
                return validationTypeDef.validationType;
            return null;
        }
        
        internal ValidationTypeDef GetValidationTypeDef (TypeDef typeDef) {
            if (typeMap.TryGetValue(typeDef, out var validationTypeDef))
                return validationTypeDef;
            return null;
        }
        
        /// <summary>
        /// Construct an immutable <see cref="ValidationSet"/> from a given <see cref="JSON.JsonTypeSchema"/> or a
        /// <see cref="Native.NativeTypeSchema"/>. The <see cref="ValidationSet"/> is intended to be used by
        /// <see cref="TypeValidator"/> to validate JSON payloads by <see cref="TypeValidator.ValidateObject"/>. 
        /// </summary>
        public ValidationSet (TypeSchema schema) {
            var schemaTypes = schema.Types;
            var typeCount   = schemaTypes.Count + 20; // 20 - roughly the number of StandardTypes
            types           = new List<ValidationTypeDef>               (typeCount);
            typeMap         = new Dictionary<TypeDef,ValidationTypeDef> (typeCount);
            
            var standardType = schema.StandardTypes;
            AddStandardType(TypeId.Boolean,     standardType.Boolean);
            AddStandardType(TypeId.String,      standardType.String);
            AddStandardType(TypeId.Uint8,       standardType.Uint8);
            AddStandardType(TypeId.Int16,       standardType.Int16);
            AddStandardType(TypeId.Int32,       standardType.Int32);
            AddStandardType(TypeId.Int64,       standardType.Int64);
            AddStandardType(TypeId.Float,       standardType.Float);
            AddStandardType(TypeId.Double,      standardType.Double);
            AddStandardType(TypeId.BigInteger,  standardType.BigInteger);
            AddStandardType(TypeId.DateTime,    standardType.DateTime);
            AddStandardType(TypeId.Guid,        standardType.Guid);
            AddStandardType(TypeId.JsonValue,   standardType.JsonValue);
            AddStandardType(TypeId.String,      standardType.JsonKey);

            foreach (var type in schemaTypes) {
                if (typeMap.ContainsKey(type))
                    continue;
                var validationTypeDef = ValidationTypeDef.Create(type);
                if (validationTypeDef == null)
                    continue;
                types.Add(validationTypeDef);
                typeMap.Add(type, validationTypeDef);
            }
            // set ValidationType references
            foreach (var type in types) {
                type.SetFields(typeMap);
                var union = type.unionType;
                union?.SetUnionTypes(typeMap);
            }
        }
        
        public ValidationType               TypeDefAsValidationType(TypeDef typeDef) => typeMap[typeDef].validationType;

        public ICollection<ValidationType>  TypeDefsAsValidationTypes(ICollection<TypeDef> types) {
            var list = new List<ValidationType>(this.types.Count);
            foreach (var typeDef in types) {
                var validationType = TypeDefAsValidationType(typeDef);
                list.Add(validationType);
            }
            return list;
        }
        
        private void AddStandardType (TypeId typeId, TypeDef typeDef) {
            if (typeDef == null)
                return;
            var typeName            = GetTypeName(typeId, out bool isNullable);
            var validationTypeDef   = new ValidationTypeDef(typeId, typeName, isNullable, typeDef);
            types.Add(validationTypeDef);
            typeMap.Add(typeDef, validationTypeDef);
        }
        
        private static string GetTypeName (TypeId typeId, out bool isNullable) {
            switch (typeId) {
                case TypeId.Uint8:      isNullable = false;     return "uint8";
                case TypeId.Int16:      isNullable = false;     return "int16";
                case TypeId.Int32:      isNullable = false;     return "int32";
                case TypeId.Int64:      isNullable = false;     return "int64";
                case TypeId.Float:      isNullable = false;     return "float";
                case TypeId.Double:     isNullable = false;     return "double";
                // --- boolean type
                case TypeId.Boolean:    isNullable = false;     return "boolean";
                // --- string types        
                case TypeId.String:     isNullable = true;      return "string";
                case TypeId.BigInteger: isNullable = false;     return "BigInteger";
                case TypeId.DateTime:   isNullable = false;     return "DateTime";
                case TypeId.Guid:       isNullable = false;     return "Guid";
                case TypeId.JsonValue:  isNullable = true;      return "JSON";
                default:
                    throw new InvalidOperationException($"no standard typeId: {typeId}");
            }
        }
    }
}