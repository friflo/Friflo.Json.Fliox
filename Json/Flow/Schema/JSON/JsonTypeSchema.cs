// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;

// ReSharper disable JoinNullCheckWithUsage
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
            
            var reader          = new ObjectReader(new TypeStore());
            
            foreach (JsonSchema schema in schemaList) {
                var context = new JsonTypeContext(schema, allSchemas, standardTypes, reader);
                var rootRef = schema.rootRef;
                if (rootRef != null) {
                    FindRef(schema.rootRef, context);
                }
                foreach (var pair in schema.typeDefs) {
                    JsonTypeDef typeDef = pair.Value;
                    JsonType    type    = typeDef.type;
                    var         extends = type.extends;
                    type.name           = pair.Key;
                    if (extends != null) {
                        typeDef.baseType = FindRef(extends.reference, context);
                    }
                    var typeType    = type.type;
                    var oneOf       = type.oneOf;
                    if (oneOf != null || typeType == "object") {
                        var properties      = type.properties;
                        if (properties != null) {
                            typeDef.fields = new List<FieldDef>(properties.Count);
                            foreach (var propPair in properties) {
                                string      fieldName   = propPair.Key;
                                FieldType   field       = propPair.Value;
                                SetField(typeDef, fieldName, field, context);
                            }
                        }
                    }
                    if (oneOf != null) {
                        var types = new List<TypeDef>(oneOf.Count);
                        foreach (var item in oneOf) {
                            var itemRef = FindRef(item.reference, context);
                            types.Add(itemRef);
                        }
                        typeDef.unionType = new UnionType (type.discriminator, types);
                    }
                }
            }
        }
        
        private static void SetField (JsonTypeDef typeDef, string fieldName, FieldType field, in JsonTypeContext context) {
            field.name              = fieldName;
            TypeDef fieldType; // not initialized by intention
            bool    isArray         = false;
            bool    isDictionary    = false;
            bool    required        = typeDef.type.required?.Contains(fieldName) ?? false;

            FieldType   items       = field.items;
            string      jsonType    = field.type.json;
            FieldType   addProps    = field.additionalProperties;
            
            if (field.reference != null) {
                fieldType = FindRef(field.reference, context);
            }
            else if (items?.reference != null) {
                isArray = true;
                fieldType = FindFieldType(items, context);
            }
            else if (field.oneOf != null) {
                TypeDef oneOfType = null; 
                foreach (var item in field.oneOf) {
                    var itemType = FindFieldType(item, context);
                    if (itemType == null)
                        continue;
                    oneOfType = itemType;
                }
                if (oneOfType == null)
                    throw new InvalidOperationException($"\"oneOf\" array without a type: {field.oneOf}");
                fieldType = oneOfType;
            }
            else if (addProps != null) {
                isDictionary = true;
                if (addProps.reference != null) {
                    fieldType = FindRef(addProps.reference, context);
                } else {
                    throw new InvalidOperationException("additionalProperties requires \"$ref\"");
                }
            }
            else if (jsonType != null) {
                fieldType = FindTypeFromJson (jsonType, items, context, ref isArray);
            }
            else if (field.discriminant != null) {
                typeDef.discriminant = field.discriminant[0]; // a discriminant has no FieldDef
                return;
            }
            else {
                fieldType = context.standardTypes.JsonValue;
                // throw new InvalidOperationException($"cannot determine field type. type: {type}, field: {field}");
            }
            var fieldDef = new FieldDef (fieldName, required, fieldType, isArray, isDictionary);
            typeDef.fields.Add(fieldDef);
        }
        
        private static TypeDef FindTypeFromJson (string json, FieldType  items, in JsonTypeContext context, ref bool isArray) {
            if     (json.StartsWith('\"')) {
                var jsonValue = json.Substring(1, json.Length - 2);
                if (jsonValue == "array") {
                    isArray = true;
                    return FindFieldType (items, context);
                }
                if (jsonValue == "null")
                    return null;
                return FindType(jsonValue, context);
            }
            if (json.StartsWith('[')) {
                // handle nullable field types
                TypeDef elementType = null;
                var fieldTypes = context.reader.Read<List<string>>(json);
                foreach (var itemType in fieldTypes) {
                    if (itemType == "null")
                        continue;
                    if (itemType == "array") {
                        return FindFieldType (items, context);
                    }
                    var elementTypeDef = FindType(itemType, context);
                    if (elementTypeDef != null) {
                        elementType = elementTypeDef;
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException("additionalProperties requires \"$ref\"");
                return elementType;
            }
            throw new InvalidOperationException($"Unexpected type: {json}");
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
                // case "null":    return null;
                // case "array":   return null;
                // case "object":  return null;
            }
            throw new InvalidOperationException($"unexpected standard type: {type}");
        }
        
        // return null if optional
        private static TypeDef FindFieldType (FieldType itemType, in JsonTypeContext context) {
            var reference = itemType.reference;
            if (reference != null) {
                if (reference.StartsWith("#/definitions/")) {
                    return context.schema.typeDefs[reference];
                }
                return context.schemas[reference];
            }
            var jsonType =  itemType.type.json;
            if (jsonType != null) {
                bool isArray = true;
                return FindTypeFromJson(jsonType, itemType.items, context, ref isArray);
            }
            throw new InvalidOperationException($"no type given for field: {itemType.name}");
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
        internal readonly   ObjectReader                    reader;

        internal JsonTypeContext(
            JsonSchema                      schema,
            Dictionary<string, JsonTypeDef> schemas,
            JsonStandardTypes               standardTypes,
            ObjectReader                    reader)
        {
            this.schema         = schema;
            this.schemas        = schemas;
            this.standardTypes  = standardTypes;
            this.reader         = reader;
        }
    }
}