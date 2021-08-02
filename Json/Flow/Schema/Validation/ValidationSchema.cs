// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class ValidationSchema : IDisposable
    {
        public  readonly    List<ValidationType>                types;
        
        private  readonly   Dictionary<TypeDef, ValidationType> typeMap;

        public ValidationSchema (TypeSchema schema) {
            var schemaTypes = schema.Types;
            var typeCount   = schemaTypes.Count + 20; // 20 - roughly the number of StandardTypes
            types           = new List<ValidationType>                  (typeCount);
            typeMap         = new Dictionary<TypeDef, ValidationType>   (typeCount);
            
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
            AddStandardType(TypeId.JsonValue,   standardType.JsonValue);

            foreach (var type in schemaTypes) {
                var validationType = new ValidationType(type);
                types.Add(validationType);
                typeMap.Add(type, validationType);
            }
            // set ValidationType references
            foreach (var type in types) {
                var fields = type.fields;
                if (fields != null) {
                    foreach (var field in fields) {
                        field.type = typeMap[field.typeDef];
                    }
                }
                var union = type.unionType;
                if (union != null) {
                    foreach (var unionType in union.unionType.types) {
                        ValidationType validationType = typeMap[unionType];
                        union.types.Add(validationType);
                    }
                }
            }
        }
        
        private void AddStandardType (TypeId typeId, TypeDef typeDef) {
            if (typeDef == null)
                return;
            var type = new ValidationType(typeId, typeDef);
            types.Add(type);
            typeMap.Add(typeDef, type);
        }

        public void Dispose() {
            foreach (var type in types) {
                type.Dispose();
            }
        }
    }
}