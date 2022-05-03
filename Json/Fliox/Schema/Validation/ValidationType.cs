// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Validation
{
    // could by a struct 
    public sealed class ValidationType  {
        public    readonly  string              fieldName;
        internal  readonly  Utf8String          name;
        public    readonly  bool                required;
        public    readonly  bool                isArray;
        public    readonly  bool                isDictionary;
        public    readonly  bool                isNullableElement;  
        public    readonly  int                 requiredPos;
        public              ValidationTypeDef   TypeDef => typeDef;
    
        // --- internal
        internal            ValidationTypeDef   typeDef;
        internal            TypeId              typeId;
        internal readonly   TypeDef             type;
        internal readonly   string              typeName;

        public  override    string              ToString() => fieldName;
        
        internal ValidationType(FieldDef fieldDef, int requiredPos) {
            type                = fieldDef.type;
            typeName            = fieldDef.isArray ? $"{type.Name}[]" : type.Name; 
            fieldName           = fieldDef.name;
            name                = fieldDef.nameUtf8;
            required            = fieldDef.required;
            isArray             = fieldDef.isArray;
            isDictionary        = fieldDef.isDictionary;
            isNullableElement   = fieldDef.isNullableElement;
            this.requiredPos    = requiredPos;
        }
    }
}