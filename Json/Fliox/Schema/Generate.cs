// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Schema
{
    /// <summary>
    /// Generate Typescript from the given options. Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema"/>
    /// </summary>
    public sealed partial class TypescriptGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".ts", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".ts", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate JSON Schema from the given options. Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema"/>
    /// </summary>
    public sealed partial class JsonSchemaGenerator
    {
        public static Generator Generate (NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".json", options.replacements, sepTypes, options.getPath);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(JsonTypeOptions options) {
            var generator   = new Generator(options.schema, options.fileExt ?? ".json", options.replacements, options.separateTypes, options.getPath);
            Generate(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate C# from the given options. Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema"/>
    /// </summary>
    public sealed partial class CSharpGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
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
    /// <see href="https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json.Tests/Common/UnitTest/Fliox/Schema"/>
    /// </summary>
    public partial class KotlinGenerator
    {
        public static Generator Generate(NativeTypeOptions options) {
            options.typeStore.AddMappers(options.rootTypes);
            var schema      = new NativeTypeSchema(options.typeStore);
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
}