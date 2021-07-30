// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Definition;
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
        public static Generator Generate(NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, ".ts", options.stripNamespaces, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, ".ts", options.stripNamespaces, options.separateTypes, options.getPath);
            Generate(generator);
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
        public static Generator Generate (NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, ".json", options.stripNamespaces, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, ".json", options.stripNamespaces, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate C# from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/tree/main/Json.Tests/Common/UnitTest/Flow/Schema"/>
    /// </summary>
    public partial class CSharpGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, ".cs", options.stripNamespaces, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
}