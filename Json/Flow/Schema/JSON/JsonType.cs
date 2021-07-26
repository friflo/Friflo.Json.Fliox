// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonType
    {
        public  List<JsonType>                  oneOf;
        //
        public  JsonValue                       type; // todo
        public  Dictionary<string, JsonType>    properties;
        public  List<string>                    required;
        public  bool                            additionalProperties;
        //
        [Fri.Property(Name = "$ref")]
        public  JsonType                        reference;
        //
        public  List<JsonType>                  items;
        //
        [Fri.Property(Name = "enum")]
        public  List<string>                    enums;
    }
}