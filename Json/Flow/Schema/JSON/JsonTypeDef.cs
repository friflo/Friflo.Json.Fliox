// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonTypeDef : TypeDef {
        private  readonly   string              name;
        internal readonly   JsonType            type;
        
        // --- TypeDef
        public   override   TypeDef             BaseType        => baseType;
        public   override   bool                IsComplex       => fields != null;
        public   override   List<Field>         Fields          => fields;
        public   override   bool                IsArray         => isArray;
        public   override   bool                IsDictionary    => isDictionary;
        public   override   UnionType           UnionType       => unionType;
        public   override   string              Discriminant    => discriminant;
        public   override   bool                IsEnum          => EnumValues != null;
        public   override   ICollection<string> EnumValues      { get; }
        public   override   TypeSemantic        TypeSemantic    => TypeSemantic.None;

        public   override   string              ToString()      => name; 

        // --- private
        private             TypeDef             baseType;
        internal            List<Field>         fields;
        internal            UnionType           unionType;
        internal            bool                isArray;
        internal            bool                isDictionary;
        internal            string              discriminant;

        public JsonTypeDef (JsonType type, string name) {
            this.name = name;
            this.type = type;
            if (type.enums != null) {
                EnumValues = type.enums; 
            }
        }

        public override bool IsDerivedField(Field field) {
            throw new System.NotImplementedException();
        }
    }
}