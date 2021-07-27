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
                var rootRef = schema.rootRef;
                if (rootRef != null) {
                    FindRef(schema.rootRef, schema, globalSchemas);
                }

                foreach (var pair in schema.typeDefs) {
                    JsonTypeDef typeDef = pair.Value;
                    JsonType    type    = typeDef.type;
                    var         extends = typeDef.type.extends;
                    type.name           = pair.Key;
                    if (extends != null) {
                        typeDef.baseType = FindRef(extends.reference, schema, globalSchemas);
                    }
                    var typeType = type.type;
                    if (typeType != null) {
                        FindType(typeType, schema, globalSchemas, null);
                    }
                    var properties      = type.properties;
                    if (properties != null) {
                        typeDef.fields = new List<FieldDef>(properties.Count);
                        foreach (var propPair in properties) {
                            string      fieldName   = propPair.Key;
                            FieldType   field       = propPair.Value;
                            field.name              = fieldName;
                            bool        requiredField   = type.required?.Contains(fieldName) ?? false;
                            var fieldDef = new FieldDef {
                                name        = fieldName,
                                required    = requiredField
                            };
                            typeDef.fields.Add(fieldDef);
                            if (field.reference != null) {
                                fieldDef.type = FindRef(field.reference, schema, globalSchemas);
                            }
                            var items = field.items;
                            if (items != null && items.reference != null) {
                                typeDef.isArray = true;
                                fieldDef.type = FindRef(items.reference, schema, globalSchemas);
                            }
                            var fieldType = field.type;
                            if (fieldType.json != null) {
                                fieldDef.type = FindType(fieldType.json, schema, globalSchemas, null);
                            }
                            var addProps = field.additionalProperties;
                            if (addProps != null) {
                                typeDef.isDictionary = true;
                                if (addProps.reference != null) {
                                    fieldDef.type = FindRef(addProps.reference, schema, globalSchemas);
                                }
                            }
                            if (field.discriminant != null) {
                                typeDef.discriminant = field.discriminant[0];
                            }
                        }
                    }
                    var oneOf = type.oneOf;
                    if (oneOf != null) {
                        var unionType = typeDef.unionType = new UnionType {
                            types           = new List<TypeDef>(oneOf.Count),
                            discriminator   = type.discriminator
                        };
                        foreach (var item in oneOf) {
                            var itemRef = FindRef(item.reference, schema, globalSchemas);
                            unionType.types.Add(itemRef);
                        }
                    }
                }
            }
        }
        
        private static TypeDef FindType (string type, JsonSchemaType schema, Dictionary<string, JsonTypeDef> schemas, StandardTypes types) {
            var standardType = StandardType(type, types);
            return standardType;
        }
        
        private static TypeDef StandardType (string type, StandardTypes types) {
            switch (type) {
                case "\"boolean\"": return null; // types.Boolean;
                case "\"string\"":  return null; // types.String;
                case "\"integer\"": return null; // types.Int32;     // todo
                case "\"number\"":  return null; // types.Double;    // todo
                case "\"array\"":   return null; // types.Double;    // todo
            }
            return null;
        }

        private static TypeDef FindRef (string reference, JsonSchemaType schema, Dictionary<string, JsonTypeDef> schemas) {
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