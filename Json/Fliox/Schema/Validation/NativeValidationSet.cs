// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Validation
{
    public class NativeValidationSet
    {
        private readonly    Dictionary<Type, ValidationType> validationTypes = new Dictionary<Type, ValidationType>();
        
        public ValidationType GetValidationType(Type type) {
            if (validationTypes.TryGetValue(type, out var validationType))
                return validationType;
            
            var nativeSchema    = NativeTypeSchema.Create(type);
            var validationSet   = new ValidationSet(nativeSchema);
            validationType      = validationSet.GetValidationType(nativeSchema, type);
            validationTypes.Add(type, validationType);

            return validationType;
        }
    }
}