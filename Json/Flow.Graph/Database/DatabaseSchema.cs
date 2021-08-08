// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    public class DatabaseSchema
    {
        private readonly    TypeSchema                          typeSchema;
        private readonly    Dictionary<string, ValidationType>  containerTypes = new Dictionary<string, ValidationType>();
        
        public DatabaseSchema(TypeSchema typeSchema, ICollection<ValidationType> entityTypes) {
            this.typeSchema = typeSchema;
            foreach (var entityType in entityTypes) {
                containerTypes.Add(entityType.name, entityType);
            }
        }

        public ValidationResult ValidateEntities (string container, Dictionary<string, EntityValue> entities, MessageContext messageContext)
        {
            var type = containerTypes[container];
            using (var pooledValidator = messageContext.pools.TypeValidator.Get()) {
                TypeValidator validator = pooledValidator.instance;
                foreach (var entity in entities) {
                    string json = entity.Value.Json;
                    if (!validator.ValidateObject(json, type, out string error)) {
                        throw new InvalidOperationException(error);   
                    }
                }
            }
            return null;
        }
    }
    
    public class ValidationResult
    {
        public CommandError                     error;
        public Dictionary<string, EntityError>  validationErrors;
    }
}