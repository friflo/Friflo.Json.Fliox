// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Validation;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    /// <summary>
    /// If a <see cref="DatabaseSchema"/> is assigned to a <see cref="EntityDatabase.schema"/> the JSON payloads of all
    /// entities used in write operations (create, upsert and patch) are validated against their expected container types.
    /// <br/>
    /// It is intended to be used for <see cref="Remote.RemoteHostDatabase"/> instances to ensure that the entities
    /// (records) in an <see cref="EntityContainer"/> always meet the expected type. So only successful validated JSON
    /// payloads are written to an <see cref="EntityContainer"/>.
    /// 
    /// This JSON type validation includes the following checks:
    /// <list type="bullet">
    ///   <item>
    ///     Check if the type of a property matches the container entity type.<br/>
    ///     E.g. A container using a type 'Article' expect the property "name" of type string. Writing the payload
    ///     <code>{ "id": "test", "name": 123 }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Incorrect type. was: 123, expect: string at Article > name</code>
    ///   </item>
    ///   <item>
    ///     Check if required properties defined in the container type are present in the JSON payload.<br/>
    ///     E.g. A container using a type 'Article' requires the property "name" being present. Writing the payload
    ///     <code>{ "id": "test" }</code> will result in the error:
    ///     <code>WriteError: Article 'test', Missing required fields: [name] at Article > (root)</code>
    ///   </item>
    ///   <item>
    ///     Check that no unknown properties are present in a JSON payload<br/>
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
            List<JsonKey>                           entityKeys,
            List<JsonValue>                         entities,
            MessageContext                          messageContext,
            EntityErrorType                         errorType,
            ref Dictionary<string, EntityErrors>    entityErrorMap
        ) {
            EntityContainer.AssertEntityCounts(entityKeys, entities);
            Dictionary<JsonKey, EntityError> validationErrors = null;
            var type = containerTypes[container];
            using (var pooledValidator = messageContext.pools.TypeValidator.Get()) {
                TypeValidator validator = pooledValidator.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity = entities[n];
                    if (entity.json == null)  // TAG_ENTITY_NULL
                        continue;
                    string json = entity.json;
                    if (!validator.ValidateObject(json, type, out string error)) {
                        var key = entityKeys[n];
                        if (validationErrors == null) {
                            validationErrors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
                        }
                        entities[n] = new JsonValue();
                        validationErrors.Add(key, new EntityError(errorType, container, key, error));
                    }
                }
            }
            if (validationErrors == null)
                return;
            var errors = SyncResponse.GetEntityErrors(ref entityErrorMap, container);
            errors.AddErrors(validationErrors);
            // foreach (var pair in validationErrors) {
            //     var key = pair.Key;
            //     entities.Remove(key);
            // }
        }
    }
 
}