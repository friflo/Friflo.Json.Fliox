// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

using Req       = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;
using Ignore    = Friflo.Json.Fliox.Mapper.Fri.IgnoreAttribute;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Fliox.Schema.JSON
{
    /// <summary>
    /// Compatible subset of JSON Schema with some extensions required for code generation.<br/>
    /// JSON Schema specification: https://json-schema.org/specification.html<br/>
    /// <br/>
    /// Following extensions are added to JSON Schema:
    /// <list type="bullet">
    ///     <item><see cref="JsonType.extends"/></item>
    ///     <item><see cref="JsonType.discriminator"/></item>
    ///     <item><see cref="JsonType.isStruct"/></item>
    ///     <item><see cref="JsonType.isAbstract"/></item>
    ///     <item><see cref="JsonType.messages"/></item>
    ///     <item><see cref="JsonType.commands"/></item>
    ///     <item><see cref="JsonType.key"/></item>
    ///     <item><see cref="FieldType.relation"/></item>
    /// </list>
    /// The restriction of <see cref="JSONSchema"/> are:
    /// <list type="bullet">
    ///   <item>
    ///     A schema property cannot nest anonymous types by "type": "object" with "properties": { ... }. <br/>
    ///     The property type needs to be a known type like "string", ... or a referenced ("$ref") type.  <br/>
    ///     This restriction enables generation of code and types for languages without support of anonymous types. <br/>
    ///     It also enables concise error messages for validation errors when using <see cref="Validation.TypeValidator"/>.
    ///   </item>
    ///   <item>
    ///     Note: Arrays and dictionaries are also valid schema properties. E.g. <br></br>
    ///     A valid array property like: <code>{ "type": ["array", "null"], "items": { "type": "string" } }</code><br></br>
    ///     A valid dictionary property like:  <code>{ "type": "object", "additionalProperties": { "type": "string" } }</code><br></br>
    ///     These element / value types needs to be a known type like "string", ... or a referenced ("$ref") type.
    ///   </item>
    ///   <item>
    ///     On root level are only "$ref": "..." and "definitions": [...] allowed.
    ///   </item>
    /// </list>
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class JSONSchema
    {
        [Fri.Property(Name =                               "$ref")]
                public      string                          rootRef;

                public      Dictionary<string, JsonType>    definitions;

        /// <summary>file name is <see cref="name"/> + ".json".
        /// E.g. <see cref="name"/>: Standard.json, <see cref="name"/>: "Standard</summary>
        [Ignore]public      string                          fileName;
        [Ignore]public      string                          name;
        [Ignore]internal    Dictionary<string, JsonTypeDef> typeDefs;

        public override string                          ToString() => fileName;
    }

    public sealed class JsonType
    {
                public  TypeRef                         extends;

                public  string                          discriminator;
                public  List<FieldType>                 oneOf;
                public  bool?                           isAbstract;
        //
             // public  SchemaType?                     type; // todo use this
                public  string                          type; // null or SchemaType
                public  string                          key;  // if null a property named "id" must exist
                public  Dictionary<string, FieldType>   properties;
                public  Dictionary<string, MessageType> commands;
                public  Dictionary<string, MessageType> messages;
                public  bool?                           isStruct;
                public  List<string>                    required;
                public  bool                            additionalProperties;
        //
        [Fri.Property(Name =                           "enum")]
                public  List<string>                    enums;

        [Ignore]public  string                          name;

                public  string                          description;

        public override string                          ToString() => name;
    }

    public sealed class TypeRef {
        [Fri.Property (Name =          "$ref")]
        [Req]   public  string          reference;
                public  string          type;   // not used - was used for nullable array elements

        public override string          ToString() => reference;
    }

    public sealed class FieldType
    {
                public  JsonValue       type;           // SchemaType or SchemaType[]

        [Fri.Property(Name =           "enum")]
                public  List<string>    discriminant;   // contains exactly one element for a specific type or a list is inside an abstract type

                public  FieldType       items;

                public  List<FieldType> oneOf;

                public  long?           minimum;
                public  long?           maximum;

                public  string          pattern;
                public  string          format;  // "date-time"

        [Fri.Property(Name =           "$ref")]
                public  string          reference;

                public  FieldType       additionalProperties;

        [Ignore]public  string          name;
        [Ignore]public  bool?           isKey;
                public  bool?           isAutoIncrement;

                public  string          relation;

                public  string          description;

        public override string          ToString() => name;
    }

    public sealed class MessageType
    {
        [Ignore]public  string          name;
                public  FieldType       param;
                public  FieldType       result;
                public  string          description;

        public override string          ToString() => name;
    }
    
    internal enum SchemaType {
        [Fri.EnumValue(Name = "null")]      Null,
        [Fri.EnumValue(Name = "object")]    Object,
        [Fri.EnumValue(Name = "string")]    String,
        [Fri.EnumValue(Name = "boolean")]   Boolean,
        [Fri.EnumValue(Name = "number")]    Number,
        [Fri.EnumValue(Name = "integer")]   Integer,
        [Fri.EnumValue(Name = "array")]     Array
    }
}