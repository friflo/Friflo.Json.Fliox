// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

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
            var typescript = new Typescript(typeStore, stripNamespaces, separateTypes);
            typescript.GenerateSchema();
            return typescript.generator;
        }
            
        public Generator JsonSchema (ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            var jsonSchema = new JsonSchema(typeStore, stripNamespaces, separateTypes);
            jsonSchema.GenerateSchema();
            return jsonSchema.generator;
        }
    }
}