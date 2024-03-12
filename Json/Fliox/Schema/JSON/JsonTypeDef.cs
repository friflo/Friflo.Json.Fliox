// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.JSON
{
    internal sealed class JsonTypeDef : TypeDef {
        private  readonly   string                      name;
        internal readonly   JsonType                    type;
        
        // --- TypeDef
        public   override   string                      AssemblyName    => null;
        public   override   TypeDef                     BaseType        => baseType;
        public   override   bool                        IsStruct        => isStruct;
        public   override   IReadOnlyList<FieldDef>     Fields          => fields;
        public   override   IReadOnlyList<MessageDef>   Messages        => messages;
        public   override   IReadOnlyList<MessageDef>   Commands        => commands;
        public   override   UnionType                   UnionType       => unionType;
        public   override   bool                        IsAbstract      => isAbstract; 
        public   override   string                      Discriminant    => discriminant;
        public   override   string                      Discriminator   => discriminator;
        public   override   string                      DiscriminatorDoc=> discriminatorDoc;
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
        internal            string              discriminatorDoc;
        internal            bool                isStruct;
        internal            bool                isAbstract;
        internal readonly   JSONSchema          schema;

        public JsonTypeDef (JsonType type, string name, string ns, string keyField, JSONSchema schema, IUtf8Buffer buffer) :
            base (name, ns, keyField, GetTypeId(type, name, ns), type.description, buffer.Add(name))
        {
            this.name   = name;
            this.type   = type;
            this.schema = schema;
            EnumValues  = EnumValue.CreateEnumValues(type.enums, type.descriptions, buffer);
        }
        
        public JsonTypeDef (string name, IUtf8Buffer buffer, StandardTypeId typeId) :
            base (name, "Standard", null, typeId, null, buffer.Add(name))
        {
            this.name   = name;
        }
        
        private static StandardTypeId GetTypeId(JsonType type, string name, string ns) {
            if (ns != "Standard") {
                return type.enums != null ? StandardTypeId.Enum : StandardTypeId.None;
            }
            switch (name) {
               case "uint8":        return StandardTypeId.Uint8;
               case "int16":        return StandardTypeId.Int16;
               case "int32":        return StandardTypeId.Int32;
               case "int64":        return StandardTypeId.Int64;
    
               // NON_CLS
               case "int8":         return StandardTypeId.Int8;
               case "uint16":       return StandardTypeId.UInt16;
               case "uint32":       return StandardTypeId.UInt32;
               case "uint64":       return StandardTypeId.UInt64;
               //
               case "float":        return StandardTypeId.Float;
               case "double":       return StandardTypeId.Double;
               case "BigInteger":   return StandardTypeId.BigInteger;
               case "DateTime":     return StandardTypeId.DateTime;
               case "Guid":         return StandardTypeId.Guid;
               case "JsonKey":      return StandardTypeId.JsonKey;
               case "JsonTable":    return StandardTypeId.JsonTable;
               default:             throw new InvalidOperationException($"unknown Standard type: {name}");
            }
        }
    }
}