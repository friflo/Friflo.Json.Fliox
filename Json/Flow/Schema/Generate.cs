// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema.Native;

namespace Friflo.Json.Flow.Schema
{
    /// <summary>
    /// Generate Typescript from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/blob/main/Json.Tests/Common/UnitTest/Flow/Schema/GenerateSchema.cs"/>
    /// </summary>
    public partial class Typescript
    {
        public static Generator Generate(TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".ts");
            var _           = new Typescript(generator);
            return generator;
        }
    }
    
    /// <summary>
    /// Generate JSON Schema from the given rootTypes/>
    /// Examples available at:
    /// <see href="https://github.com/friflo/Friflo.Json.Flow/blob/main/Json.Tests/Common/UnitTest/Flow/Schema/GenerateSchema.cs"/>
    /// </summary>
    public partial class JsonSchema
    {
        public static Generator Generate (TypeStore typeStore, ICollection<Type> rootTypes, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".json");
            var _           = new JsonSchema(generator);
            return generator;
        }
    }
}