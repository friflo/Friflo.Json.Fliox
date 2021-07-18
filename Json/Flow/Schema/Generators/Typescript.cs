// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Generators
{
    public class Typescript
    {
        private readonly    Generator   generator;

        public Typescript (ICollection<Type> types, string folder, TypeStore typeStore) {
            generator = new Generator(types, folder, typeStore);
        }
        
        public void GenerateSchema() {
            foreach (var pair in generator.typeMappers) {
                var mapper = pair.Value;
                EmitType(mapper);
            }
        }
        
        private void EmitType(TypeMapper mapper) {
            
        }
    }
}