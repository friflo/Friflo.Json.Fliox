// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Validation
{
    internal sealed class ValidationUnion  {
        private   readonly  UnionType   unionType;
        internal  readonly  string      discriminatorStr;
        internal  readonly  Utf8String  discriminator;
        private   readonly  UnionItem[] types;
        internal            string      TypesAsString { get; private set; }

        public   override   string      ToString()      => discriminatorStr;

        internal ValidationUnion(UnionType union) {
            unionType           = union;
            discriminatorStr    = $"'{union.discriminator}'";
            discriminator       = union.discriminatorUtf8;
            types               = new UnionItem[union.types.Count];
        }

        internal void SetUnionTypes(Dictionary<TypeDef, ValidationTypeDef> typeMap) {
            int n = 0;
            foreach (var unionItem in unionType.types) {
                ValidationTypeDef validationType = typeMap[unionItem.typeDef];
                var item = new UnionItem(unionItem.discriminant, unionItem.discriminantUtf8, validationType);
                types[n++] = item;
            }
            TypesAsString       = GetTypesAsString();
        }
        
        internal static bool FindUnion (ValidationUnion union, in Bytes discriminant, out ValidationTypeDef typeDef) {
            var types = union.types;
            for (int n = 0; n < types.Length; n++) {
                if (types[n].discriminant.IsEqual(discriminant)) {
                    typeDef    = types[n].typeDef;
                    return true;
                }
            }
            typeDef    = null;
            return false;
        }
        
        private string GetTypesAsString() {
            var sb = new StringBuilder();
            bool first = true;
            sb.Clear();
            sb.Append('[');
            foreach (var type in types) {
                if (first) {
                    first = false;
                } else {
                    sb.Append(", ");
                }
                sb.Append(type.discriminantStr);
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
    
    internal readonly struct UnionItem
    {
        internal  readonly  string              discriminantStr;
        internal  readonly  Utf8String          discriminant;
        internal  readonly  ValidationTypeDef   typeDef;

        public    override  string              ToString() => discriminantStr;

        internal UnionItem (string discriminant, in Utf8String discriminantUtf8, ValidationTypeDef typeDef) {
            discriminantStr     = discriminant ?? throw new ArgumentNullException(nameof(discriminant));
            this.discriminant   = discriminantUtf8;
            this.typeDef        = typeDef;
        }
    }
}