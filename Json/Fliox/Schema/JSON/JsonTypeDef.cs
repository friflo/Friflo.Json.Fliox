// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.JSON
{
    internal sealed class JsonTypeDef : TypeDef {
        private  readonly   string                      name;
        internal readonly   JsonType                    type;
        
        // --- TypeDef
        public   override   TypeDef                     BaseType        => baseType;
        public   override   bool                        IsClass         => fields != null;
        public   override   bool                        IsStruct        => isStruct;
        public   override   IReadOnlyList<FieldDef>     Fields          => fields;
        public   override   IReadOnlyList<MessageDef>   Messages        => messages;
        public   override   IReadOnlyList<MessageDef>   Commands        => commands;
        public   override   UnionType                   UnionType       => unionType;
        public   override   bool                        IsAbstract      => isAbstract; 
        public   override   string                      Discriminant    => discriminant;
        public   override   string                      Discriminator   => discriminator;
        public   override   bool                        IsEnum          => EnumValues != null;
        public   override   IReadOnlyList<EnumValue>    EnumValues      { get; }

        public   override   string              ToString()      => name; 

        // --- private
        internal            JsonTypeDef         baseType;
        internal            List<FieldDef>      fields;
        internal            List<MessageDef>    messages;
        internal            List<MessageDef>    commands;
        internal            UnionType           unionType;
        internal            string              discriminant;
        internal            string              discriminator;
        internal            bool                isStruct;
        internal            bool                isAbstract;
        internal readonly   JSONSchema          schema;

        public JsonTypeDef (JsonType type, string name, string ns, JSONSchema schema) :
            base (name, ns, type.description)
        {
            this.name   = name;
            this.type   = type;
            this.schema = schema;
            EnumValues  = EnumValue.CreateEnumValues(type.enums, type.descriptions);
        }
        
        public JsonTypeDef (string name) :
            base (name, null, null)
        {
            this.name = name;
        }
    }
}