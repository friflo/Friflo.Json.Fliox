// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonTypeSchema : TypeSchema
    {
        public  override    ICollection<TypeDef>    Types           { get; }
        public  override    StandardTypes           StandardTypes   { get; }
        public  override    ICollection<TypeDef>    SeparateTypes   { get; }
        
        public JsonTypeSchema(List<JsonSchema> schemaList) {
            var allSchemas = new Dictionary<string, JsonTypeDef>(schemaList.Count);
            foreach (JsonSchema schema in schemaList) {
                schema.typeDefs = new Dictionary<string, JsonTypeDef>(schema.definitions.Count);
                foreach (var pair in schema.definitions) {
                    var typeName    = pair.Key;
                    var type        = pair.Value;
                    var typeDef     = new JsonTypeDef (type, typeName);
                    var schemaId = $"./{schema.name}#/definitions/{typeName}";
                    allSchemas.Add(schemaId, typeDef);
                    var localId = $"#/definitions/{typeName}";
                    schema.typeDefs.Add(localId, typeDef);
                }
            }
            
            Types               = new List<TypeDef>(allSchemas.Values);
            var standardTypes   = new JsonStandardTypes(allSchemas);
            StandardTypes       = standardTypes;
            
            foreach (JsonSchema schema in schemaList) {
                var context = new JsonTypeContext(schema, allSchemas, standardTypes);
                var rootRef = schema.rootRef;
                if (rootRef != null) {
                    FindRef(schema.rootRef, context);
                }
                foreach (var pair in schema.typeDefs) {
                    JsonTypeDef typeDef = pair.Value;
                    JsonType    type    = typeDef.type;
                    var         extends = typeDef.type.extends;
                    type.name           = pair.Key;
                    if (extends != null) {
                        typeDef.baseType = FindRef(extends.reference, context);
                    }
                    var typeType = type.type;
                    if (typeType != null) {
                        FindType(typeType, context);
                    }
                    AssignProperties(typeDef, type, context);
                    
                    var oneOf = type.oneOf;
                    if (oneOf != null) {
                        var unionType = typeDef.unionType = new UnionType {
                            types           = new List<TypeDef>(oneOf.Count),
                            discriminator   = type.discriminator
                        };
                        foreach (var item in oneOf) {
                            var itemRef = FindRef(item.reference, context);
                            unionType.types.Add(itemRef);
                        }
                    }
                }
            }
        }
        
        private static bool AssignProperties (JsonTypeDef typeDef, JsonType type, in JsonTypeContext context) {
            var properties      = type.properties;
            if (properties == null)
                return false;
            
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
                    fieldDef.type = FindRef(field.reference, context);
                }
                var items = field.items;
                if (items != null && items.reference != null) {
                    typeDef.isArray = true;
                    fieldDef.type = FindRef(items.reference, context);
                }
                var jsonType = field.type.json;
                if (jsonType != null) {
                    if (jsonType.StartsWith('\"')) {
                        var jsonValue = jsonType.Substring(1, jsonType.Length - 2); 
                        fieldDef.type = FindType(jsonValue, context);
                    }
                }
                var addProps = field.additionalProperties;
                if (addProps != null) {
                    typeDef.isDictionary = true;
                    if (addProps.reference != null) {
                        fieldDef.type = FindRef(addProps.reference, context);
                    }
                }
                if (field.discriminant != null) {
                    typeDef.discriminant = field.discriminant[0];
                }
            }
            return true;
        }
        
        private static TypeDef FindType (string type, in JsonTypeContext context) {
            var standardType = StandardType(type, context.standardTypes);
            return standardType;
        }
        
        private static TypeDef StandardType (string type, JsonStandardTypes types) {
            switch (type) {
                case "boolean": return types.Boolean;
                case "string":  return types.String;
                case "integer": return types.Int32;
                case "number":  return types.Double;
                case "array":   return null;
                case "object":  return null;
            }
            return null;
        }

        private static TypeDef FindRef (string reference, in JsonTypeContext context) {
            if (reference.StartsWith("#/definitions/")) {
                return context.schema.typeDefs[reference];
            }
            return context.schemas[reference];
        }

        public static TypeSchema FromFolder(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var jsonSchemas = new Dictionary<string, string>(fileNames.Length);
            foreach (var fileName in fileNames) {
                var schemaName = fileName.Substring(folder.Length + 1);
                var schema = File.ReadAllText(fileName, Encoding.UTF8);
                jsonSchemas.Add(schemaName, schema);
            }
            return FromSchemas(jsonSchemas);
        }
        
        public static TypeSchema FromSchemas(Dictionary<string, string> jsonSchemas) {
            var schemas = new List<JsonSchema>(jsonSchemas.Count);
            var reader = new ObjectReader(new TypeStore());
            foreach (var jsonSchema in jsonSchemas) {
                var schema = reader.Read<JsonSchema>(jsonSchema.Value);
                schema.name = jsonSchema.Key;
                schemas.Add(schema);
            }
            var typeSchema = new JsonTypeSchema(schemas);
            return typeSchema;
        }
    }
    
    internal readonly struct JsonTypeContext
    {
        internal readonly   JsonSchema                      schema;
        internal readonly   Dictionary<string, JsonTypeDef> schemas;
        internal readonly   JsonStandardTypes               standardTypes;
        
        internal JsonTypeContext(JsonSchema schema, Dictionary<string, JsonTypeDef> schemas, JsonStandardTypes standardTypes) {
            this.schema         = schema;
            this.schemas        = schemas;
            this.standardTypes  = standardTypes;
        }
    }
}