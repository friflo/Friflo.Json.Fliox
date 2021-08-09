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
        private readonly    Dictionary<string, ValidationType>  containerTypes;
        
        public DatabaseSchema(TypeSchema typeSchema, ICollection<ValidationType> entityTypes) {
            this.typeSchema = typeSchema;
            containerTypes = new Dictionary<string, ValidationType>(entityTypes.Count);
            foreach (var entityType in entityTypes) {
                containerTypes.Add(entityType.name, entityType);
            }
        }

        public void ValidateEntities (
            string                                  container,
            Dictionary<string, EntityValue>         entities,
            MessageContext                          messageContext,
            EntityErrorType                         errorType,
            ref Dictionary<string, EntityErrors>    entityErrorMap
        ) {
            Dictionary<string, EntityError> validationErrors = null;
            var type = containerTypes[container];
            using (var pooledValidator = messageContext.pools.TypeValidator.Get()) {
                TypeValidator validator = pooledValidator.instance;
                foreach (var entity in entities) {
                    string json = entity.Value.Json;
                    if (!validator.ValidateObject(json, type, out string error)) {
                        string key = entity.Key;
                        if (validationErrors == null) {
                            validationErrors = new Dictionary<string, EntityError>();
                        }
                        validationErrors.Add(key, new EntityError(errorType, container, key, error));
                    }
                }
            }
            if (validationErrors == null)
                return;
            var errors = SyncResponse.GetEntityErrors(ref entityErrorMap, container);
            errors.AddErrors(validationErrors);
            foreach (var pair in validationErrors) {
                var key = pair.Key;
                entities.Remove(key);
            }
        }
    }
 
}