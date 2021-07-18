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
            
        public void CreateSchema (ICollection<Type> rootTypes, string folder) {
            foreach (var type in rootTypes) {
                typeStore.GetTypeMapper(type);
            }
            
            var typescript = new Typescript(rootTypes, $"{folder}/Typescript", typeStore);
            typescript.GenerateSchema();
            
            var jsonSchema = new JsonSchema(rootTypes, $"{folder}/JSON", typeStore);
            jsonSchema.GenerateSchema();
        }
    }
}