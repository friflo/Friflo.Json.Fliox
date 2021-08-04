// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;

namespace Friflo.Json.Flow.Schema.Validation
{
    public static class ValidationExtension
    {
        public static ValidationType TypeAsValidationType<T>(this NativeTypeSchema nativeSchema, ValidationSet validationSet) {
            var type = typeof(T);
            var typeDef = nativeSchema.TypeAsTypeDef(type);
            return validationSet.TypeDefAsValidationType(typeDef);
        }
        
        public static ValidationType TypeAsValidationType<T>(this JsonTypeSchema jsonSchema, ValidationSet validationSet, string @namespace = null) {
            var type = typeof(T);
            @namespace = @namespace ?? type.Namespace;
            var path = $"./{@namespace}.{type.Name}.json#/definitions/{type.Name}";
            var typeDef = jsonSchema.TypeAsTypeDef(path);
            return validationSet.TypeDefAsValidationType(typeDef);
        }
    } 
}