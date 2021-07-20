// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Generators;

namespace Friflo.Json.Flow.Schema
{
    public class SchemaGenerator
    {
        public readonly TypeStore typeStore = new TypeStore();
        
        public SchemaGenerator (ICollection<Type> rootTypes) {
            foreach (var type in rootTypes) {
                typeStore.GetTypeMapper(type);
            }
        }
        
        public Generator Typescript() {
            var generator = new Generator(typeStore);
            var typescript = new Typescript(generator);
            typescript.GenerateSchema();
            return generator;
        }
            
        public Generator JsonSchema () {
            var generator = new Generator(typeStore);
            var jsonSchema = new JsonSchema(generator);
            jsonSchema.GenerateSchema();
            return generator;
        }
    }
}