// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Validation
{
    public class NativeValidationSet
    {
        private readonly    Dictionary<Type, ValidationType> validationTypes = new Dictionary<Type, ValidationType>();
        
        public ValidationType GetValidationType(Type type) {
            if (validationTypes.TryGetValue(type, out var validationType)) {
                return validationType;
            }
            validationType = GetValidationTypeInternal(type);
            validationTypes.Add(type, validationType);
            return validationType;
        }
        
        private static ValidationType  GetValidationTypeInternal(Type type) {
            var nativeSchema    = NativeTypeSchema.Create(type);
            var validationSet   = new ValidationSet(nativeSchema);
            var attr            = nativeSchema.GetArgAttributes(type);
            var typeDef         = attr.typeDef;
            var fieldDef        = new FieldDef("param", attr.required, false, false, typeDef, attr.isArray, attr.isDictionary, false, null, null, null, nativeSchema.Utf8Buffer);
            var validationType  = new ValidationType(fieldDef, -1);
            if (attr.isArray || attr.isDictionary) {
                var elementMapper       = typeDef.mapper.GetElementMapper();
                var elementTypeDef      = nativeSchema.GetArgAttributes(elementMapper.type);
                validationType.typeDef = validationSet.GetValidationTypeDef(elementTypeDef.typeDef);
            } else {
                validationType.typeDef = validationSet.GetValidationTypeDef(typeDef);
            }
            return validationType;
        }
    }
}