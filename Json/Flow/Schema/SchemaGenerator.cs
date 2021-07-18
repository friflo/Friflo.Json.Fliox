// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Generators;

namespace Friflo.Json.Flow.Schema
{
    public class SchemaGenerator
    {
        public void CreateSchema (Type type, string folder) {
            var typeStore = new TypeStore();
            typeStore.GetTypeMapper(type);
            
            var typescript = new Typescript(type, $"{folder}/Typescript", typeStore);
            typescript.GenerateSchema();
            
            var jsonSchema = new JsonSchema(type, $"{folder}/JSON", typeStore);
            jsonSchema.GenerateSchema();
        }
    }
}