// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Schema.Definition;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class ValidationSchema : IDisposable
    {
        public  readonly    List<ValidationType>    types;

        public ValidationSchema (TypeSchema schema) {
            var schemaTypes = schema.Types;
            types           = new List<ValidationType>(schemaTypes.Count);
            foreach (var type in schemaTypes) {
                var validationType = new ValidationType(type);
                types.Add(validationType);
            }
        }

        public void Dispose() {
            foreach (var type in types) {
                type.Dispose();
            }
        }
    }
}