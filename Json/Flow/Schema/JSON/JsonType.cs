// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonType : TypeDef
    {
        public  string                          disciminator;
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

        // --- TypeDef
        public  override    TypeDef             BaseType        { get; }
        public  override    bool                IsComplex       => fields != null;
        public  override    List<Field>         Fields          => fields;
        public  override    bool                IsArray         => items != null;
        public  override    bool                IsDictionary    { get; }
        public  override    UnionType           UnionType       { get; }
        public  override    string              Discriminant    { get; }
        public  override    bool                IsEnum          => enums != null;
        public  override    ICollection<string> EnumValues      => enums;
        public  override    TypeSemantic        TypeSemantic    => TypeSemantic.None;
        
        // --- private
        private             List<Field>         fields;

        public void Init () {
            if (properties != null) {
                fields = new List<Field>(properties.Count);
                foreach (var property in properties) {
                    string  fieldName       = property.Key;
                    bool    requiredField   = required?.Contains(fieldName) ?? false;  
                    var field = new Field {
                        name        = fieldName,
                        required    = requiredField,
                        // type =  todo  
                    };
                    fields.Add(field);
                }
            }
        }

        public  override    bool                IsDerivedField(Field field) {
            throw new System.NotImplementedException();
        }
    }
}