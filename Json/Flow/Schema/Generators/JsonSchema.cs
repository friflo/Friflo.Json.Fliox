// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema.Generators
{
    public class JsonSchema
    {
        private readonly    Generator   generator;


        public JsonSchema (Type type, string folder, TypeStore typeStore) {
            generator = new Generator(type, folder, typeStore);
        }
        
        public void GenerateSchema() {
            
        }
    }
}