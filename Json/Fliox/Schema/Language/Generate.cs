// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema.Language
{
    /// <summary>
    /// Generate Typescript from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public sealed partial class TypescriptGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".d.ts", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".d.ts", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate HTML from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public sealed partial class HtmlGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".html", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".html", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate JSON Schema from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public sealed partial class JsonSchemaGenerator
    {
        public static Generator Generate (NativeTypeOptions o) {
            var schema      = NativeTypeSchema.Create(o.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(o.separateTypes);
            var generator   = new Generator(schema, o.fileExt ?? ".json", o.replacements, sepTypes, o.getPath, o.databaseUrl);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions o) {
            var generator   = new Generator(o.schema, o.fileExt ?? ".json", o.replacements, o.separateTypes, o.getPath, o.databaseUrl);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate C# from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public sealed partial class CSharpGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".cs", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".cs", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate Kotlin from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public partial class KotlinGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".kt", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".kt", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate GraphQL from the given options. Examples available at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema">Schema unit tests</a>
    /// </summary>
    public sealed partial class GraphQLGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".d.ts", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".d.ts", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    public partial class MarkdownGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var generator   = new Generator(schema, options.fileExt ?? ".md", options.replacements, null, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".md", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
}