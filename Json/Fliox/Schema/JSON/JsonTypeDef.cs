// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.JSON
{
    public class JsonTypeDef : TypeDef {
        private  readonly   string              name;
        internal readonly   JsonType            type;
        
        // --- TypeDef
        public   override   TypeDef             BaseType        => baseType;
        public   override   bool                IsClass         => fields != null;
        public   override   bool                IsStruct        => isStruct;
        public   override   List<FieldDef>      Fields          => fields;
        public   override   UnionType           UnionType       => unionType;
        public   override   bool                IsAbstract      => isAbstract; 
        public   override   string              Discriminant    => discriminant;
        public   override   string              Discriminator   => discriminator;
        public   override   bool                IsEnum          => EnumValues != null;
        public   override   ICollection<string> EnumValues      { get; }
        public   override   TypeSemantic        TypeSemantic    => TypeSemantic.None;

        public   override   string              ToString()      => name; 

        // --- private
        internal            JsonTypeDef         baseType;
        internal            List<FieldDef>      fields;
        internal            UnionType           unionType;
        internal            string              discriminant;
        internal            string              discriminator;
        internal            bool                isStruct;
        internal            bool                isAbstract;

        public JsonTypeDef (JsonType type, string name, string ns) :
            base (name, ns)
        {
            this.name   = name;
            this.type   = type;
            if (type.enums != null) {
                EnumValues = type.enums; 
            }
        }
        
        public JsonTypeDef (string name) :
            base (name, null)
        {
            this.name = name;
        }
    }
}