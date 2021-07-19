// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
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
            var sb = new StringBuilder();
            foreach (var pair in generator.typeMappers) {
                var mapper = pair.Value;
                sb.Clear();
                var result = EmitType(mapper, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
        }
        
        private static EmitResult EmitType(TypeMapper mapper, StringBuilder sb) {
            var underlyingMapper = mapper.GetUnderlyingMapper();
            if (underlyingMapper != null) {
                mapper = underlyingMapper;
            }
            if (mapper.IsComplex) {
                var fields = mapper.propFields.fields;
                sb.AppendLine($"class {mapper.type.Name} {{");
                foreach (var field in fields) {
                    sb.AppendLine($"    {field} : {field.fieldType.type.Name}");
                }
                sb.AppendLine("}");
                return new EmitResult(mapper, sb.ToString());
            }
            return null;
        }
    }
}