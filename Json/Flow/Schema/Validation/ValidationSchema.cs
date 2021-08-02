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
            types           = new List<ValidationType>                  (schemaTypes.Count);
            typeMap         = new Dictionary<TypeDef, ValidationType>   (schemaTypes.Count);
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
                        field.type = typeMap[field.type.typeDef];
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

        public void Dispose() {
            foreach (var type in types) {
                type.Dispose();
            }
        }
    }
}