// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonSchemaType
    {
        [Fri.Ignore]
        public  string                          name;
        public  Dictionary<string, JsonType>    definitions;
        [Fri.Ignore]
        public  Dictionary<string, JsonTypeDef> typeDefs;

        public override string                  ToString() => name;
    }
    
    public class JsonType
    {
        [Fri.Ignore]
        public  string                          name;
        
        public  RefType                         extends;
        
        public  string                          discriminator;
        public  List<FieldType>                 oneOf;
        //
        // public  SchemaType?                  type; // todo use this
        public  string                          type; // null or SchemaType
        public  Dictionary<string, FieldType>   properties;
        public  List<string>                    required;
        public  bool                            additionalProperties;
        //
        [Fri.Property(Name =                   "$ref")]
        public  JsonType                        reference;
        //
        [Fri.Property(Name =                   "enum")]
        public  List<string>                    enums;

        public override string                  ToString() => name;
    }
    
    public class RefType {
        [Fri.Property(Name =   "$ref")]
        public  string          reference;
    }
    
    public class FieldType
    {
        [Fri.Ignore]
        public  string          name;
        
        public  JsonValue       type;           // SchemaType or SchemaType[]
        
        [Fri.Property(Name =   "enum")]
        public  List<string>    discriminant;   // contains exactly one element
        
        public  FieldType       items;
        
        public  long            minimum;
        public  long            maximum;
        
        public  string          pattern;
        public  string          format;  // "date-time"

        [Fri.Property(Name =   "$ref")]
        public  string          reference;

        public  FieldType       additionalProperties;
        
        public override string  ToString() => name;
    }
    
    public enum SchemaType {
        [Fri.EnumValue(Name = "null")]
        Null,
        [Fri.EnumValue(Name = "object")]
        Object,
        [Fri.EnumValue(Name = "string")]
        String,
        [Fri.EnumValue(Name = "boolean")]
        Boolean,
        [Fri.EnumValue(Name = "number")]
        Number,
        [Fri.EnumValue(Name = "integer")]
        Integer,
        [Fri.EnumValue(Name = "array")]
        Array
    }
}