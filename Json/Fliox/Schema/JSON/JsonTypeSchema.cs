// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable JoinNullCheckWithUsage
namespace Friflo.Json.Fliox.Schema.JSON
{
    /// <summary>
    /// <see cref="JsonTypeSchema"/> is used to create an immutable <see cref="TypeSchema"/> instance
    /// from a set of given <see cref="JSONSchema"/>'s.<br/>
    /// The utility method <see cref="JsonTypeSchema.ReadSchemas"/> can be used to read a set of
    /// <see cref="JSONSchema"/>'s as files in a folder.
    /// </summary>
    public sealed class JsonTypeSchema : TypeSchema, IDisposable
    {
        public  override    IReadOnlyList<TypeDef>          Types           { get; }
        public  override    StandardTypes                   StandardTypes   { get; }
        public  override    TypeDef                         RootType        { get; }
        
        private readonly    Dictionary<string, JsonTypeDef> typeMap;
        
        public JsonTypeSchema(List<JSONSchema> schemaList, string rootType = null) {
            typeMap = new Dictionary<string, JsonTypeDef>();
            foreach (JSONSchema schema in schemaList) {
                schema.typeDefs = new Dictionary<string, JsonTypeDef>(schema.definitions.Count);
                foreach (var pair in schema.definitions) {
                    var typeName    = pair.Key;
                    var type        = pair.Value;
                    var @namespace  = GetNamespace(schema, typeName);
                    var typeDef     = new JsonTypeDef (type, typeName, @namespace, type.key, schema, Utf8Buffer);
                    var schemaId    = $"./{schema.fileName}#/definitions/{typeName}";
                    typeMap.Add(schemaId, typeDef);
                    var localId = $"#/definitions/{typeName}";
                    schema.typeDefs.Add(localId, typeDef);
                }
            }
            
            var standardTypes   = new JsonStandardTypes(typeMap, Utf8Buffer);
            StandardTypes       = standardTypes;
            
            using (var typeStore    = new TypeStore())
            using (var reader       = new ObjectReader(typeStore))
            {
                foreach (var pair in typeMap) {
                    JsonTypeDef typeDef = pair.Value;
                    JsonType    type    = typeDef.type;
                    var schema          = typeDef.schema; 
                    var context = new JsonTypeContext(schema, typeMap, standardTypes, reader, Utf8Buffer);
                    var rootRef = schema.rootRef;
                    if (rootRef != null) {
                        FindRef(schema.rootRef, context);
                    }
                    
                    var         extends     = type.extends;
                    type.name               = pair.Key;
                    if (extends != null) {
                        typeDef.baseType = FindRef(extends.reference, context);
                    }
                    var typeType    = type.type;
                    var oneOf       = type.oneOf;
                    if (oneOf != null || typeType == "object") {
                        typeDef.isAbstract  = type.isAbstract.HasValue && type.isAbstract.Value;
                        typeDef.isStruct    = type.isStruct.HasValue && type.isStruct.Value;
                        var properties      = type.properties;
                        if (properties != null) {
                            typeDef.fields   = new List<FieldDef>(properties.Count);
                            foreach (var propPair in properties) {
                                string      fieldName   = propPair.Key;
                                FieldType   field       = propPair.Value;
                                // discriminator field is not a real member -> skip it
                                if (type.discriminator == fieldName)
                                    continue;
                                SetField(typeDef, fieldName, field, context);
                            }
                            typeDef.SetFieldMap();
                        }
                        typeDef.commands = CreateMessages(type.commands, true,  context);
                        typeDef.messages = CreateMessages(type.messages, false, context);
                    }
                    if (oneOf != null) {
                        // Check is required to support standard.JsonKey in JsonSchemaGenerator at:
                        // AddType (map, standard.JsonKey,     "\"oneOf\": [{ \"type\": \"string\" }, { \"type\": \"integer\" }]");
                        if (type.discriminator != null) {
                            var unionTypes = new List<UnionItem>(oneOf.Count);
                            foreach (var item in oneOf) {
                                var itemRef             = FindRef(item.reference, context);
                                var discriminantMember  = itemRef.type.properties[type.discriminator];
                                var discriminant        = discriminantMember.discriminant[0];
                                var unionItem           = new UnionItem(itemRef, discriminant, Utf8Buffer);
                                unionTypes.Add(unionItem);
                            }
                            typeDef.isAbstract  = true;
                            var discField       = type.properties[type.discriminator];
                            typeDef.unionType   = new UnionType (type.discriminator, discField.description, unionTypes, Utf8Buffer);
                        }
                    }
                }
                foreach (JSONSchema schema in schemaList) {
                    foreach (var pair in schema.typeDefs) {
                        JsonTypeDef typeDef = pair.Value;
                        if (typeDef.discriminant == null)
                            continue;
                        var baseType = typeDef.baseType;
                        while (baseType != null) {
                            var unionType = baseType.unionType;
                            if (unionType != null) {
                                typeDef.discriminator       = unionType.discriminator;
                                typeDef.discriminatorDoc    = unionType.doc;
                                break;
                            }
                            baseType = baseType.baseType;
                        }
                        if (typeDef.discriminator == null)
                            throw new InvalidOperationException($"found no discriminator in base classes. type: {typeDef}");
                    }
                }
            }
            var types = new List<TypeDef>(typeMap.Values);
            MarkDerivedFields(types);
            if (rootType != null) {
                var rootTypeDef = TypeAsTypeDef(rootType);
                if (rootTypeDef == null)
                    throw new InvalidOperationException($"rootType not found: {rootType}");
                rootTypeDef.SetEntityKeyFields();
                SetRelationTypes(rootTypeDef, types);
                RootType = rootTypeDef;
            }
            Types = OrderTypes(RootType, types);
        }
        
        public void Dispose() { }

        private static void SetField (JsonTypeDef typeDef, string fieldName, FieldType field, in JsonTypeContext context) {
            field.name      = fieldName;
            if (field.discriminant != null) {
                typeDef.discriminant = field.discriminant[0]; // a discriminant has no FieldDef
                return;
            }
            bool required       = typeDef.type.required?.Contains(fieldName) ?? false;
            var  attr           = new FieldAttributes();
            var  fieldType      = GetFieldType(field, ref attr, context);
            var isAutoIncrement = field.isAutoIncrement.HasValue && field.isAutoIncrement.Value;
            var relation        = field.relation;

            var fieldDef = new FieldDef (fieldName, null, required, isAutoIncrement, fieldType, null,
                attr.isArray, attr.isDictionary, attr.isNullableElement, typeDef, relation, field.description, context.utf8Buffer);
            typeDef.fields.Add(fieldDef);
        }
        
        private static TypeDef GetFieldType (FieldType field, ref FieldAttributes attr, in JsonTypeContext context)
        {
            FieldType   items       = GetItemsFieldType(field.items, out attr.isNullableElement);
            JsonValue   jsonType    = field.type;
            FieldType   addProps    = field.additionalProperties;
            if (field.reference != null) {
                // "$ref": "./Standard.json#/definitions/int64"  | ...
                return FindRef(field.reference, context);
            }
            if (items?.reference != null) {
                // "items": { "type": "string" } | { "$ref": "#/definitions/CustomTypeName" }  | ...
                attr.isArray = true;
                return FindTypeFromJson(field, jsonType, items, context, ref attr);
            }
            if (field.oneOf != null) {
                // "oneOf": [{ "$ref": "./Standard.json#/definitions/uint8" }, {"type": "null"}]  | ...
                TypeDef oneOfType = null; 
                foreach (var item in field.oneOf) {
                    if (item.type.AsString() == "\"null\"") {
                        attr.isNullable = true;
                        continue;
                    }
                    // var itemType = FindTypeFromJson(field, jsonType, item, context, ref attr);
                    var itemType = FindFieldType(field, item, context);
                    if (itemType == null)
                        continue;
                    oneOfType = itemType;
                }
                if (oneOfType == null)
                    throw new InvalidOperationException($"'oneOf' array without a type: {field.oneOf}");
                return oneOfType;
            }
            if (addProps != null) {
                attr.isDictionary = true;
                if (addProps.reference != null) {
                    // "additionalProperties": { "$ref": "#/definitions/CustomTypeName" }  | ...
                    return FindRef(addProps.reference, context);
                }
                // "additionalProperties": {  }
                return FindTypeFromJson(field, jsonType, items, context, ref attr);
            }
            if (!jsonType.IsNull()) {
                // "type": "string" | ["string", "null"] | "array" | ["array", "null]  | ...
                return FindTypeFromJson(field, jsonType, items, context, ref attr);
            }
            // throw new InvalidOperationException($"cannot determine field type. type: {type}, field: {field}");
            return context.standardTypes.JsonValue;
        }
        
        private static List<MessageDef> CreateMessages (Dictionary<string, MessageType> signatures, bool isCommand, in JsonTypeContext context) {
            if (signatures == null)
                return null;
            var messages = new List<MessageDef>(signatures.Count);
            foreach (var msgPair in signatures) {
                string      messageName = msgPair.Key;
                MessageType signature   = msgPair.Value;
                var m = CreateMessage(messageName, signature, isCommand, context);
                messages.Add(m);
            }
            return messages;
        }
        
        private static MessageDef CreateMessage (string commandName, MessageType signature, bool isCommand, in JsonTypeContext context) {
            signature.name  = commandName;
            var valueType   = GetMessageArg("param",   signature.param,  context);
            var resultType  = GetMessageArg("result",  signature.result, context);
            if (isCommand && resultType == null)
                throw new ArgumentException($"missing result. command: {commandName}");
            return new MessageDef(commandName, valueType, resultType, signature.description);
        }
        
        private static FieldDef GetMessageArg(string name, FieldType fieldType, in JsonTypeContext context) {
            if (fieldType == null)
                return null; 
            var attr        = new FieldAttributes();
            var argType     = GetFieldType(fieldType, ref attr, context);
            var required    = !attr.isNullable;
            return new FieldDef(name, null, required, false, argType, null, attr.isArray, attr.isDictionary, false, null, null, null, context.utf8Buffer);
        }
        
        private static TypeDef FindTypeFromJson (
            FieldType           field,
            in JsonValue        jsonArray,
            FieldType           items,
            in JsonTypeContext  context,
            ref FieldAttributes attr)
        {
            if (jsonArray.IsNull()) {
                return FindFieldType (field, items, context);
            }
            var json = jsonArray.AsString();
            if (json.StartsWith("\"")) {
                var jsonValue = json.Substring(1, json.Length - 2);
                switch (jsonValue) {
                    case "array":
                        attr.isArray = true;
                        return FindFieldType (field, items, context);
                    case "null":
                        return null; // null is not a type. Cause intentionally a NullReferenceException
                    default:
                        return FindType(jsonValue, context);
                }
            }
            if (json.StartsWith("[")) {
                // handle nullable field types
                TypeDef elementType = null;
                var fieldTypes = context.reader.Read<List<string>>(json);
                foreach (var itemType in fieldTypes) {
                    switch (itemType) {
                        case "null":
                            attr.isNullable = true;
                            continue;
                        case "array":
                            attr.isArray = true;
                            continue;
                    }
                    var elementTypeDef = FindType(itemType, context);
                    if (elementTypeDef != null) {
                        elementType = elementTypeDef;
                    }
                }
                if (attr.isArray) {
                    return FindFieldType (field, items, context);
                }
                if (elementType == null)
                    throw new InvalidOperationException("additionalProperties requires '$ref'");
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
                case "object":  return types.JsonValue;
                // case "null":    return null;
                // case "array":   return null;
            }
            throw new InvalidOperationException($"unexpected standard type: {type}");
        }
        
        // return null if optional
        private static TypeDef FindFieldType (FieldType field, FieldType itemType, in JsonTypeContext context) {
            var reference = itemType.reference;
            if (reference != null) {
                if (reference.StartsWith("#/definitions/")) {
                    return context.schema.typeDefs[reference];
                }
                return context.schemas[reference];
            }
            var jsonType =  itemType.type;
            if (!jsonType.IsNull()) {
                var attr            = new FieldAttributes();
                var itemTypeItems   = GetItemsFieldType(itemType.items, out _);
                return FindTypeFromJson(field, jsonType, itemTypeItems, context, ref attr);
            }
            return context.standardTypes.JsonValue;
        }
        
        private static readonly JsonValue Null = new JsonValue("\"null\"");
        
        /// Supporting nullable (value type) array elements seems uh - however it is supported. Reasons against:
        /// <list type="bullet">
        ///   <item>Application now have to check for null when accessing these types of arrays -> uh</item>
        ///   <item>Generated languages have typically no support for custom nullable values types.
        ///         Common element types like int, byte, ... are typically supported - custom types not.</item>
        /// </list>
        // ReSharper disable once UnusedMember.Local
        private static FieldType GetItemsFieldType (FieldType itemType, out bool isNullableElement) {
            if (itemType == null) {
                isNullableElement = false;
                return null;
            }
            if (!itemType.type.IsNull()) {
                isNullableElement = false;
                return itemType;
            }
            if (itemType.reference != null) {
                isNullableElement = false;
                return itemType;
            }
            var oneOf = itemType.oneOf;
            if (oneOf != null) {
                isNullableElement = false;
                FieldType elementType = null;
                foreach (var fieldType in oneOf) {
                    if (fieldType.type.IsEqual(Null)) {
                        isNullableElement = true;
                    }
                    if (fieldType.reference != null) {
                        if (elementType != null)
                            throw new InvalidOperationException($"Found multiple '$ref' in 'oneOf': {fieldType.reference}");        
                        elementType = fieldType;
                    }
                }
                if (elementType == null)
                    throw new InvalidOperationException("Missing '$ref' in 'oneOf'");
                return elementType;
            }
            throw new InvalidOperationException("Expected 'type', '$ref' or 'oneOf'");
        }

        private static JsonTypeDef FindRef (string reference, in JsonTypeContext context) {
            if (reference.StartsWith("#/definitions/")) {
                return context.schema.typeDefs[reference];
            }
            return context.schemas[reference];
        }
        
        private static string GetNamespace (JSONSchema schema, string typeName) {
            var name = schema.name;
            var rootRef = schema.rootRef;
            if (rootRef != null) {
                if (!rootRef.StartsWith("#/definitions/"))
                    throw new InvalidOperationException($"Expect root '$ref' starts with: #/definitions/. was: {rootRef}");
                var rootTypeName = rootRef.Substring("#/definitions/".Length);
                if (rootTypeName == typeName) {
                    name = name.Substring(0, name.Length - typeName.Length - 1); // -1 => '.'
                }
            }
            return name;
        }

        /// <summary>Read a set of <see cref="JSONSchema"/>'s stored as files in the given <paramref name="folder"/>.</summary>
        public static List<JSONSchema> ReadSchemas(string folder) {
            var fileNames = Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly);
            // Sort() of schema files not schema relevant. But useful to maintain a deterministic order of types to
            // simplify: validation of schemas by tests, merging different version using git, easier to memorize by humans. 
            Array.Sort(fileNames);
            var schemas = new List<JSONSchema>();
            using (var typeStore    = new TypeStore())
            using (var reader       = new ObjectReader(typeStore)) {
                foreach (var path in fileNames) {
                    var fileName = path.Substring(folder.Length + 1);
                    if (fileName == "openapi.json")
                        continue;
                    var name = fileName.Substring(0, fileName.Length - ".json".Length);
                    var jsonSchema = File.ReadAllText(path, Encoding.UTF8);
                    var schema = reader.Read<JSONSchema>(jsonSchema);
                    schema.fileName = fileName;
                    schema.name = name;
                    schemas.Add(schema);
                }
                return schemas;
            }
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
        
        public TypeDef TypeAsTypeDef(string type) {
            return typeMap[type];
        }
    }
    
    internal struct FieldAttributes
    {
        internal    bool    isNullable;
        internal    bool    isArray;
        internal    bool    isDictionary;
        internal    bool    isNullableElement;
    }
    
    internal readonly struct JsonTypeContext
    {
        internal readonly   JSONSchema                      schema;
        internal readonly   Dictionary<string, JsonTypeDef> schemas;
        internal readonly   JsonStandardTypes               standardTypes;
        internal readonly   ObjectReader                    reader;
        internal readonly   IUtf8Buffer                     utf8Buffer;

        internal JsonTypeContext (
            JSONSchema                      schema,
            Dictionary<string, JsonTypeDef> schemas,
            JsonStandardTypes               standardTypes,
            ObjectReader                    reader,
            IUtf8Buffer                     utf8Buffer)
        {
            this.schema         = schema;
            this.schemas        = schemas;
            this.standardTypes  = standardTypes;
            this.reader         = reader;
            this.utf8Buffer     = utf8Buffer;
        }
    }
}