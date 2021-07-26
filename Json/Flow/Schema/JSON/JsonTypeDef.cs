// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonTypeDef : TypeDef {
        private readonly    string              name;
        private readonly    JsonType            type;
        private readonly    FieldType           fieldType;
        
        // --- TypeDef
        public  override    TypeDef             BaseType        => baseType;
        public  override    bool                IsComplex       => fields != null;
        public  override    List<Field>         Fields          => fields;
        public  override    bool                IsArray         { get; }
        public  override    bool                IsDictionary    { get; }
        public  override    UnionType           UnionType       { get; }
        public  override    string              Discriminant    { get; }
        public  override    bool                IsEnum          => EnumValues != null;
        public  override    ICollection<string> EnumValues      { get; }
        public  override    TypeSemantic        TypeSemantic    => TypeSemantic.None;

        public  override    string              ToString()      => name; 

        // --- private
        private             TypeDef             baseType;
        private readonly    List<Field>         fields;

        public JsonTypeDef (JsonType type, string name) {
            this.name = name;
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
            if (type.enums != null) {
                EnumValues = type.enums; 
            }
            if (type.oneOf != null) {
                var types = new List<TypeDef>(type.oneOf.Count);
                foreach (var item in type.oneOf) {
                    // types.Add(i); todo
                }
                UnionType = new UnionType {
                   types            = types,
                   discriminator    = type.discriminator
                };
            }
        }
        
        public JsonTypeDef (FieldType type, string name) {
            this.name = name;
            fieldType = type;
            if (type.items != null) {
                IsArray = true;
            }
            if (type.additionalProperties != null) {
                IsDictionary = true;
            }
            if (type.discriminant != null) {
                Discriminant = type.discriminant[0];
            }
        }

        public override bool IsDerivedField(Field field) {
            throw new System.NotImplementedException();
        }
    }
}