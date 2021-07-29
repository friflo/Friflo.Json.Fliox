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
        public  override    ICollection<TypeDef>            Types           { get; }
        public  override    StandardTypes                   StandardTypes   { get; }
        
        private readonly    Dictionary<string, JsonTypeDef> typeMap;
        
        public JsonTypeSchema(List<JsonFlowSchema> schemaList) {
            typeMap = new Dictionary<string, JsonTypeDef>(schemaList.Count);
            foreach (JsonFlowSchema schema in schemaList) {
                schema.typeDefs = new Dictionary<string, JsonTypeDef>(schema.definitions.Count);
                foreach (var pair in schema.definitions) {
                    var typeName    = pair.Key;
                    var type        = pair.Value;
                    var @namespace  = GetNamespace(schema, typeName);
                    var typeDef     = new JsonTypeDef (type, typeName, @namespace);
                    var schemaId = $"./{schema.fileName}#/definitions/{typeName}";
                    typeMap.Add(schemaId, typeDef);
                    var localId = $"#/definitions/{typeName}";
                    schema.typeDefs.Add(localId, typeDef);
                }
            }
            
            Types               = new List<TypeDef>(typeMap.Values);
            var standardTypes   = new JsonStandardTypes(typeMap);
            StandardTypes       = standardTypes;
            
            var reader          = new ObjectReader(new TypeStore());
            
            foreach (JsonFlowSchema schema in schemaList) {
                var context = new JsonTypeContext(schema, typeMap, standardTypes, reader);
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
                        typeDef.isStruct    = type.isStruct;
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
                        var unionTypes = new List<TypeDef>(oneOf.Count);
                        foreach (var item in oneOf) {
                            var itemRef = FindRef(item.reference, context);
                            unionTypes.Add(itemRef);
                        }
                        typeDef.unionType = new UnionType (type.discriminator, unionTypes);
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
            var fieldDef = new FieldDef (fieldName, required, fieldType, isArray, isDictionary, typeDef);
            typeDef.fields.Add(fieldDef);
        }
        
        private static TypeDef FindTypeFromJson (string json, FieldType  items, in JsonTypeContext context, ref bool isArray) {
            if     (json.StartsWith("\"")) {
                var jsonValue = json.Substring(1, json.Length - 2);
                if (jsonValue == "array") {
                    isArray = true;
                    return FindFieldType (items, context);
                }
                if (jsonValue == "null")
                    return null;
                return FindType(jsonValue, context);
            }
            if (json.StartsWith("[")) {
                // handle nullable field types
                TypeDef elementType = null;
                var fieldTypes = context.reader.Read<List<string>>(json);
                foreach (var itemType in fieldTypes) {
                    if (itemType == "null")
                        continue;
                    if (itemType == "array") {
                        isArray = true;
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
        
        private static string GetNamespace (JsonFlowSchema schema, string typeName) {
            var name = schema.name;
            var rootRef = schema.rootRef;
            if (rootRef != null) {
                if (!rootRef.StartsWith("#/definitions/"))
                    throw new InvalidOperationException($"Expect root \"$ref\" starts with: #/definitions/. was: {rootRef}");
                var rootTypeName = rootRef.Substring("#/definitions/".Length);
                if (rootTypeName == typeName) {
                    name = name.Substring(0, name.Length - typeName.Length - 1); // -1 => '.'
                }
            }
            return name;
        }

        public static List<JsonFlowSchema> FromFolder(string folder) {
            string[] fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            var schemas = new List<JsonFlowSchema>();
            var reader = new ObjectReader(new TypeStore());
            foreach (var path in fileNames) {
                var fileName = path.Substring(folder.Length + 1);
                var name = fileName.Substring(0, fileName.Length - ".json".Length);
                var jsonSchema = File.ReadAllText(path, Encoding.UTF8);
                var schema = reader.Read<JsonFlowSchema>(jsonSchema);
                schema.fileName = fileName;
                schema.name = name;
                schemas.Add(schema);
            }
            return schemas;
        }
        
        private static List<JsonFlowSchema> FromSchemas(Dictionary<string, string> jsonSchemas) {
            var schemas = new List<JsonFlowSchema>(jsonSchemas.Count);
            var reader = new ObjectReader(new TypeStore());
            foreach (var jsonSchema in jsonSchemas) {
                var schema = reader.Read<JsonFlowSchema>(jsonSchema.Value);
                schema.fileName = jsonSchema.Key;
                schemas.Add(schema);
            }
            return schemas;
        }
        
        public ICollection<TypeDef> TypesAsTypeDefs(ICollection<string> types) {
            if (types == null)
                return null;
            var list = new List<TypeDef> (types.Count);
            foreach (var type in types) {
                var typeDef = typeMap[type];
                list.Add(typeDef);
            }
            return list;
        }
    }
    
    internal readonly struct JsonTypeContext
    {
        internal readonly   JsonFlowSchema                      schema;
        internal readonly   Dictionary<string, JsonTypeDef> schemas;
        internal readonly   JsonStandardTypes               standardTypes;
        internal readonly   ObjectReader                    reader;

        internal JsonTypeContext(
            JsonFlowSchema                      schema,
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