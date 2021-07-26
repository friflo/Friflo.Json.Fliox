// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonTypeDef : TypeDef {
        private readonly    JsonType            type;
        private readonly    FieldType           fieldType;
        
        // --- TypeDef
        public  override    TypeDef             BaseType        { get; }
        public  override    bool                IsComplex       => fields != null;
        public  override    List<Field>         Fields          => fields;
        public  override    bool                IsArray         => fieldType?.items != null;
        public  override    bool                IsDictionary    { get; }
        public  override    UnionType           UnionType       { get; }
        public  override    string              Discriminant    { get; }
        public  override    bool                IsEnum          => type.enums != null;
        public  override    ICollection<string> EnumValues      => type.enums;
        public  override    TypeSemantic        TypeSemantic    => TypeSemantic.None;
        
        // --- private
        private readonly    List<Field>         fields;

        public JsonTypeDef (JsonType type) {
            this.type = type;
            var properties = type.properties;
            if (properties != null) {
                fields = new List<Field>(properties.Count);
                foreach (var property in properties) {
                    string  fieldName       = property.Key;
                    bool    requiredField   = type.required?.Contains(fieldName) ?? false;  
                    var field = new Field {
                        name        = fieldName,
                        required    = requiredField,
                        // type =  todo  
                    };
                    fields.Add(field);
                }
            }
        }

        public override bool IsDerivedField(Field field) {
            throw new System.NotImplementedException();
        }
    }
}