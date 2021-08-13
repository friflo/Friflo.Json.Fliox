// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    /// <summary>
    /// If <see cref="DatabaseSchema"/> is assigned to a <see cref="EntityDatabase.schema"/> the JSON payloads of all
    /// entities used in write operations (create, update and patch) are validated by their expected type.<br/>
    /// It is intended to be used for <see cref="Remote.RemoteHostDatabase"/> instances to ensure that the entities
    /// (records) in an <see cref="EntityContainer"/> meet the expected type. So only successful validated JSON
    /// payloads are written to an <see cref="EntityContainer"/>.
    /// 
    /// This type validation includes the following checks:
    /// <list type="bullet">
    ///   <item>
    ///     Check if the type of a property matches the type in a schema.<br/>
    ///     E.g. A container using a type 'Article' expect the property "name" of type string. Writing the payload
    ///     <code>{ "id": "test", "name": 123 }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Incorrect type. was: 123, expect: string at Article > name</code>
    ///   </item>
    ///   <item>
    ///     Check if required properties are present in the JSON payload.<br/>
    ///     E.g. A container using a type 'Article' requires the property "name" being present. Writing the payload
    ///     <code>{ "id": "test" }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Missing required fields: [name] at Article > (root)</code>
    ///   </item>
    ///   <item>
    ///     Check no unknown properties are present in a JSON payload<br/>
    ///     E.g. A container using a type 'Article' expect only the properties 'id' and 'name'. Writing the payload
    ///     <code>{ "id": "test", "name": "Phone", "foo": "Bar" }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Unknown property: 'foo' at Article > foo</code>
    ///   </item>
    /// </list>   
    /// </summary>
    public class DatabaseSchema : IDisposable
    {
        public   readonly   TypeSchema                          typeSchema;
        private  readonly   Dictionary<string, ValidationType>  containerTypes;
        private  readonly   ValidationSet                       validationSet;
        
        public DatabaseSchema(TypeSchema typeSchema) {
            validationSet   = new ValidationSet(typeSchema);
            this.typeSchema = typeSchema;
            var entityTypes = validationSet.GetEntityTypes();
            containerTypes  = new Dictionary<string, ValidationType>(entityTypes.Count);
            foreach (var entityType in entityTypes) {
                containerTypes.Add(entityType.name, entityType);
            }
        }
        
        public void Dispose() {
            validationSet.Dispose();
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