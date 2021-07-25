// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Native;

namespace Friflo.Json.Flow.Schema
{
    /// <summary>
    /// Utility class to generate a schema or code from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/blob/main/Json.Tests/Common/UnitTest/Flow/Schema/GenerateSchema.cs"/>
    /// </summary>
    public class GeneratorSchema
    {
        private readonly TypeStore typeStore;

        public GeneratorSchema (TypeStore typeStore, ICollection<Type> rootTypes) {
            this.typeStore = typeStore;
            typeStore.AddMappers(rootTypes);
        }
        
        public Generator Typescript(ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".ts");
            var typescript = new Typescript(generator);
            typescript.GenerateSchema();
            return typescript.generator;
        }
            
        public Generator JsonSchema (ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".json");
            var jsonSchema  = new JsonSchema(generator);
            jsonSchema.GenerateSchema();
            return jsonSchema.generator;
        }
    }
}