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
        public static Generator Generate(TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(separateTypes);
            var generator   = new Generator(schema, ".ts", stripNamespaces, sepTypes);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(TypeSchema schema, ICollection<string> stripNamespaces = null, ICollection<TypeDef> separateTypes = null) {
            var generator   = new Generator(schema, ".ts", stripNamespaces, separateTypes);
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
        public static Generator Generate (TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore);
            var sepTypes    = schema.TypesAsTypeDefs(separateTypes);
            var generator   = new Generator(schema, ".json", stripNamespaces, sepTypes);
            Generate(generator);
            return generator;
        }
        
        public static Generator Generate(TypeSchema schema, ICollection<string> stripNamespaces = null, ICollection<TypeDef> separateTypes = null) {
            var generator   = new Generator(schema, ".json", stripNamespaces, separateTypes);
            Generate(generator);
            return generator;
        }
    }
}