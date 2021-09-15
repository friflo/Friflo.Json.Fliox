// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Native;
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
        private  readonly   Dictionary<string, ValidationType>  containerTypes = new Dictionary<string, ValidationType>();
        private  readonly   List<ValidationSet>                 validationSets = new List<ValidationSet>();
        
        public DatabaseSchema(TypeSchema typeSchema) {
            this.typeSchema = typeSchema;
            AddTypeSchema(typeSchema);
            // AddTypeSchema(typeof(SequenceStore));
        }
        
        public void Dispose() {
            foreach (var validationSet in validationSets) {
                validationSet.Dispose();
            }
        }
        
        public void AddTypeSchema(Type rootType) {
            using (var typeStore    = new TypeStore()) {
                var nativeSchema    = new NativeTypeSchema(typeStore, rootType);
                AddTypeSchema(nativeSchema);
            }
        }
        
        public void AddTypeSchema(TypeSchema typeSchema) {
            var validationSet   = new ValidationSet(typeSchema);
            validationSets.Add(validationSet);
            var entityTypes     = validationSet.GetEntityTypes();
            foreach (var entityType in entityTypes) {
                containerTypes.Add(entityType.name, entityType);
            }
        }

        public string ValidateEntities (
            string                                  container,
            List<JsonKey>                           entityKeys,
            List<JsonValue>                         entities,
            MessageContext                          messageContext,
            EntityErrorType                         errorType,
            ref Dictionary<string, EntityErrors>    entityErrorMap
        ) {
            EntityContainer.AssertEntityCounts(entityKeys, entities);
            Dictionary<JsonKey, EntityError> validationErrors = null;
            if (!containerTypes.TryGetValue(container, out ValidationType type)) {
                return $"No Schema definition for container Type: {container}";
            }
            using (var pooledValidator = messageContext.pools.TypeValidator.Get()) {
                TypeValidator validator = pooledValidator.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity = entities[n];
                    // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                    if (!validator.ValidateObject(entity.json, type, out string error)) {
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
                return null;
            var errors = SyncResponse.GetEntityErrors(ref entityErrorMap, container);
            errors.AddErrors(validationErrors);
            
            // Remove invalid entries from entities
            int pos = 0;
            for (int n = 0; n < entities.Count; n++) {
                var entity = entities[n];
                if (entity.json == null)
                    continue;
                entities  [pos] = entity;
                entityKeys[pos] = entityKeys[n];
                pos++;
            }
            int count = entities.Count;
            entities.RemoveRange(pos,   count);
            entityKeys.RemoveRange(pos, count);
            return null;
        }
    }
 
}