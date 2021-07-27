// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Native;

namespace Friflo.Json.Flow.Schema
{
    /// <summary>
    /// Generate Typescript from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/tree/main/Json.Tests/Common/UnitTest/Flow/Schema"/>
    /// </summary>
    public partial class TypescriptGenerator
    {
        public static Generator Generate(TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".ts");
            var _           = new TypescriptGenerator(generator);
            return generator;
        }
        
        public static Generator Generate(TypeSchema schema, ICollection<string> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            var generator   = new Generator(schema, stripNamespaces, ".ts");
            var _           = new TypescriptGenerator(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate JSON Schema from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/tree/main/Json.Tests/Common/UnitTest/Flow/Schema"/>
    /// </summary>
    public partial class JsonSchemaGenerator
    {
        public static Generator Generate (TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".json");
            var _           = new JsonSchemaGenerator(generator);
            return generator;
        }
        
        public static Generator Generate(TypeSchema schema, ICollection<string> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            var generator   = new Generator(schema, stripNamespaces, ".json");
            var _           = new JsonSchemaGenerator(generator);
            return generator;
        }
    }
}