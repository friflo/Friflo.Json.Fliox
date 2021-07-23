// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema
{
    public class GeneratorSchema
    {
        private readonly TypeStore typeStore;
        
        public GeneratorSchema (TypeStore typeStore, ICollection<Type> rootTypes) {
            this.typeStore = typeStore;
            typeStore.AddMappers(rootTypes);
        }
        
        public Generator Typescript(string stripNamespace, ICollection<Type> separateTypes) {
            var typescript = new Typescript(typeStore, stripNamespace, separateTypes);
            typescript.GenerateSchema();
            return typescript.generator;
        }
            
        public Generator JsonSchema (string stripNamespace, ICollection<Type> separateTypes) {
            var jsonSchema = new JsonSchema(typeStore, stripNamespace, separateTypes);
            jsonSchema.GenerateSchema();
            return jsonSchema.generator;
        }
    }
}