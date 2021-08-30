// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.JSON;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Validation
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
        
        public static ICollection<ValidationType> TypesAsValidationTypes(this NativeTypeSchema nativeSchema, ValidationSet validationSet, ICollection<Type> types) {
            var list = new List<ValidationType>();
            foreach (var type in types) {
                var typeDef         = nativeSchema.TypeAsTypeDef(type);
                var validationType  = validationSet.TypeDefAsValidationType(typeDef);
                list.Add(validationType);
            }
            return list;
        }
    } 
}