// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Validation
{
    // could by a struct 
    public sealed class ValidationType  {
        public    readonly  ShortString                 fieldName;
        internal  readonly  Utf8String                  name;
        internal  readonly  bool                        required;
        internal  readonly  bool                        isArray;
        internal  readonly  bool                        isDictionary;
        internal  readonly  bool                        isNullableElement;  
        internal  readonly  int                         requiredPos;
        public              IEnumerable<ValidationType> Fields  => typeDef.Fields;
    
        // --- internal
        internal            ValidationTypeDef           typeDef; // null: if used as field type
        internal            TypeId                      typeId;
        internal readonly   TypeDef                     type;
        internal readonly   string                      typeName;

        public  override    string                      ToString() => fieldName.IsNull() ? typeDef.name : $"{fieldName.AsString()} : {typeDef.name}";
        
        internal ValidationType(FieldDef fieldDef, int requiredPos) {
            type                = fieldDef.type;
            typeName            = fieldDef.isArray ? $"{type.Name}[]" : type.Name; 
            fieldName           = new ShortString(fieldDef.name);
            name                = fieldDef.nameUtf8;
            required            = fieldDef.required;
            isArray             = fieldDef.isArray;
            isDictionary        = fieldDef.isDictionary;
            isNullableElement   = fieldDef.isNullableElement;
            this.requiredPos    = requiredPos;
        }
        
        internal ValidationType(ValidationTypeDef typeDef, bool isNullable, bool isArray, bool isDictionary, bool isNullableElement) {
            this.typeDef            = typeDef;
            typeId                  = typeDef.typeId;
            type                    = typeDef.typeDef;
            typeName                = type.Name; 
            fieldName               = default;
            name                    = default;
            this.required           = !isNullable;
            this.isArray            = isArray;
            this.isDictionary       = isDictionary;
            this.isNullableElement  = isNullableElement;
            requiredPos             = -1;
        }
    }
}