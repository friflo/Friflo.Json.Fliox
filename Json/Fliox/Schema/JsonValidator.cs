// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Schema.Validation;
using Friflo.Json.Fliox.Utils;

namespace Friflo.Json.Fliox.Schema
{
    public static class JsonValidator
    {
        private static readonly NativeValidationSet         ValidationSet   = new ();
        private static readonly ObjectPool<TypeValidator>   ValidatorPool   = new (() => new TypeValidator());
        
        public static bool Validate(string value, Type type, out string error) {
            var jsonValue = new JsonValue(value);
            return Validate(jsonValue, type, out error);
        }
            
        public static bool Validate(in JsonValue value, Type type, out string error) {
            var validationType  = ValidationSet.GetValidationType(type);
            using (var pooled = ValidatorPool.Get()) {
                var validator                   = pooled.instance;
                validator.qualifiedTypeErrors   = false;
                return validator.Validate(value, validationType, out error);
            }
        }
    }
}