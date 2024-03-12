// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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

            // NON_CLS
            AddStandardType(TypeId.Int8,        standardType.Int8);
            AddStandardType(TypeId.UInt16,      standardType.UInt16);
            AddStandardType(TypeId.UInt32,      standardType.UInt32);
            AddStandardType(TypeId.UInt64,      standardType.UInt64);
            
            AddStandardType(TypeId.Float,       standardType.Float);
            AddStandardType(TypeId.Double,      standardType.Double);
            AddStandardType(TypeId.BigInteger,  standardType.BigInteger);
            AddStandardType(TypeId.DateTime,    standardType.DateTime);
            AddStandardType(TypeId.Guid,        standardType.Guid);
            AddStandardType(TypeId.JsonValue,   standardType.JsonValue);
            AddStandardType(TypeId.JsonKey,     standardType.JsonKey);
            AddStandardType(TypeId.String,      standardType.ShortString);
            AddStandardType(TypeId.JsonTable,   standardType.JsonTable);

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
            var validationTypeDef   = GetStandardTypeDef(typeId, typeDef);
            types.Add(validationTypeDef);
            typeMap.Add(typeDef, validationTypeDef);
        }
        
        private static ValidationTypeDef GetStandardTypeDef (TypeId typeId, TypeDef typeDef) {
            switch (typeId) {
                // --- number type
                case TypeId.Uint8:      return new ValidationTypeDef(typeId, "uint8",       typeDef, false);
                case TypeId.Int16:      return new ValidationTypeDef(typeId, "int16",       typeDef, false);
                case TypeId.Int32:      return new ValidationTypeDef(typeId, "int32",       typeDef, false);
                case TypeId.Int64:      return new ValidationTypeDef(typeId, "int64",       typeDef, false);
                
                // NON_CLS
                case TypeId.Int8:       return new ValidationTypeDef(typeId, "int8",        typeDef, false);
                case TypeId.UInt16:     return new ValidationTypeDef(typeId, "uint16",      typeDef, false);
                case TypeId.UInt32:     return new ValidationTypeDef(typeId, "uint32",      typeDef, false);
                case TypeId.UInt64:     return new ValidationTypeDef(typeId, "uint64",      typeDef, false);
                
                case TypeId.Float:      return new ValidationTypeDef(typeId, "float",       typeDef, false);
                case TypeId.Double:     return new ValidationTypeDef(typeId, "double",      typeDef, false);
                // --- boolean type
                case TypeId.Boolean:    return new ValidationTypeDef(typeId, "boolean",     typeDef, false);
                // --- string types        
                case TypeId.String:     return new ValidationTypeDef(typeId, "string",      typeDef, true);
                case TypeId.BigInteger: return new ValidationTypeDef(typeId, "BigInteger",  typeDef, false);
                case TypeId.DateTime:   return new ValidationTypeDef(typeId, "DateTime",    typeDef, false);
                case TypeId.Guid:       return new ValidationTypeDef(typeId, "Guid",        typeDef, false);
                //
                case TypeId.JsonKey:    return new ValidationTypeDef(typeId, "JsonKey",     typeDef, false);
                // --- JSON: number, string, boolean, array & object
                case TypeId.JsonValue:  return new ValidationTypeDef(typeId, "JSON",        typeDef);
                case TypeId.JsonTable:  return new ValidationTypeDef(typeId, "JsonTable",   typeDef);
                default:
                    throw new InvalidOperationException($"no standard typeId: {typeId}");
            }
        }
    }
}