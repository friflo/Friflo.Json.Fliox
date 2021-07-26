// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonTypeSchema
    {
        public JsonTypeSchema(List<JsonSchemaType> schemaList) {
            var globalSchemas = new Dictionary<string, JsonTypeDef>(schemaList.Count);
            foreach (JsonSchemaType schema in schemaList) {
                schema.typeDefs = new Dictionary<string, JsonTypeDef>(schema.definitions.Count);
                foreach (var pair in schema.definitions) {
                    var typeName    = pair.Key;
                    var type        = pair.Value;
                    var typeDef     = new JsonTypeDef (type, typeName);
                    var schemaId = $"./{schema.name}#/definitions/{typeName}";
                    globalSchemas.Add(schemaId, typeDef);
                    var localId = $"#/definitions/{typeName}";
                    schema.typeDefs.Add(localId, typeDef);
                }
            }
            foreach (JsonSchemaType schema in schemaList) {
                foreach (var pair in schema.typeDefs) {
                    JsonTypeDef    typeDef = pair.Value;
                    var properties      = typeDef.type.properties;
                    if (properties != null) {
                        typeDef.fields = new List<Field>(properties.Count);
                        foreach (var propPair in properties) {
                            string      fieldName   = propPair.Key;
                            FieldType   fieldType   = propPair.Value;
                            fieldType.name          = fieldName;
                            bool        requiredField   = typeDef.type.required?.Contains(fieldName) ?? false;
                            var field = new Field {
                                name        = fieldName,
                                required    = requiredField
                            };
                            typeDef.fields.Add(field);
                            if (fieldType.reference != null) {
                                field.type = Find(fieldType.reference, schema, globalSchemas);
                            }
                            var items = fieldType.items;
                            if (items != null && items.reference != null) {
                                typeDef.isArray = true;
                                field.type = Find(items.reference, schema, globalSchemas);
                            }
                            var addProps = fieldType.additionalProperties;
                            if (addProps != null) {
                                typeDef.isDictionary = true;
                                if (addProps.reference != null) {
                                    field.type = Find(addProps.reference, schema, globalSchemas);
                                }
                            }
                            if (fieldType.discriminant != null) {
                                typeDef.discriminant = fieldType.discriminant[0];
                            }
                        }
                    }
                    var oneOf = typeDef.type.oneOf;
                    if (oneOf != null) {
                        var unionType = typeDef.unionType = new UnionType {
                            types           = new List<TypeDef>(oneOf.Count),
                            discriminator   = typeDef.type.discriminator
                        };
                        foreach (var item in oneOf) {
                            var type = Find(item.reference, schema, globalSchemas);
                            unionType.types.Add(type);
                        }
                    }
                }
            }
        }
        
        private static TypeDef Find (string reference, JsonSchemaType schema, Dictionary<string, JsonTypeDef> schemas) {
            if (reference.StartsWith("#/definitions/")) {
                return schema.typeDefs[reference];
            }
            return schemas[reference];
        }

        public static JsonTypeSchema FromFolder(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var jsonSchemas = new Dictionary<string, string>(fileNames.Length);
            foreach (var fileName in fileNames) {
                var schemaName = fileName.Substring(folder.Length + 1);
                var schema = File.ReadAllText(fileName, Encoding.UTF8);
                jsonSchemas.Add(schemaName, schema);
            }
            return FromSchemas(jsonSchemas);
        }
        
        public static JsonTypeSchema FromSchemas(Dictionary<string, string> jsonSchemas) {
            var schemas = new List<JsonSchemaType>(jsonSchemas.Count);
            var reader = new ObjectReader(new TypeStore());
            foreach (var jsonSchema in jsonSchemas) {
                var schema = reader.Read<JsonSchemaType>(jsonSchema.Value);
                schema.name = jsonSchema.Key;
                schemas.Add(schema);
            }
            var typeSchema = new JsonTypeSchema(schemas);
            return typeSchema;
        }
    }
}