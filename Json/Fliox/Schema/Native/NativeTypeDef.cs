// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Native
{
    internal sealed class NativeTypeDef : TypeDef
    {
        // --- internal
        internal readonly   Type                        native;
        internal readonly   TypeMapper                  mapper;
        internal            NativeTypeDef               baseType;
        internal            List<FieldDef>              fields;
        internal            List<MessageDef>            messages;
        internal            List<MessageDef>            commands;
        internal            UnionType                   unionType;
        internal            string                      discriminator;
        internal            string                      discriminatorDoc;
        internal            bool                        isAbstract;
        
        // --- TypeDef
        public   override   string                      AssemblyName    => native.Assembly.FullName;
        public   override   TypeDef                     BaseType        => baseType;
        public   override   bool                        IsEnum          { get; }
        public   override   bool                        IsStruct        { get; }
        public   override   IReadOnlyList<FieldDef>     Fields          => fields;
        public   override   IReadOnlyList<MessageDef>   Messages        => messages;
        public   override   IReadOnlyList<MessageDef>   Commands        => commands;
        public   override   string                      Discriminant    { get; }
        public   override   string                      Discriminator   => discriminator;
        public   override   string                      DiscriminatorDoc=> discriminatorDoc;
        public   override   UnionType                   UnionType       => unionType;
        public   override   bool                        IsAbstract      => isAbstract;
        public   override   IReadOnlyList<EnumValue>    EnumValues      { get; }
        
        public   override   string                      ToString()      => $"{Namespace} {Name}";
        
        public NativeTypeDef (TypeMapper mapper, string name, string @namespace, string keyField, StandardTypeId typeId, IUtf8Buffer buffer) :
            base(name, @namespace, keyField, typeId, mapper.docs, buffer.Add(name)) 
        {
            this.native     = mapper.type;
            this.mapper     = mapper;
            IsEnum          = native.IsEnum;
            IsStruct        = mapper.type.IsValueType && mapper.type != typeof(JsonKey) && mapper.type != typeof(ShortString); // JsonKey is "nullable"
            Discriminant    = mapper.Discriminant;
            EnumValues      = EnumValue.CreateEnumValues(mapper.GetEnumValues(), mapper.GetEnumValueDocs(), buffer);
        }
        
        public override bool Equals(object obj) {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            var other = (NativeTypeDef)obj;
            return native == other.native;
        }

        public override int GetHashCode() {
            return (native != null ? native.GetHashCode() : 0);
        }
    }
}