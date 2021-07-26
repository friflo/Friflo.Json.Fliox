// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonType
    {
        public  string                          discriminator;
        public  List<FieldType>                 oneOf;
        //
        public  string                          type; // null, "null", "object", "string", "number", "integer", "array"
        public  Dictionary<string, FieldType>   properties;
        public  List<string>                    required;
        public  bool                            additionalProperties;
        //
        [Fri.Property(Name = "$ref")]
        public  JsonType                        reference;
        //
        [Fri.Property(Name = "enum")]
        public  List<string>                    enums;
    }
    
    public class FieldType
    {
        public  string          type; // "object", "string", "number", "integer", "array", []
        
        public  List<FieldType> items;
        
        [Fri.Property(Name = "$ref")]
        public  FieldType       reference;

        public  FieldType       additionalProperties;
    }
}