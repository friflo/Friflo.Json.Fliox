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
            generator       = new Generator(types, folder, typeStore);
        }
        
        public void GenerateSchema() {
            var sb = new StringBuilder();
            // emit custom types
            foreach (var pair in generator.typeMappers) {
                var mapper = pair.Value;
                sb.Clear();
                var result = EmitType(mapper, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            
            generator.GroupTypesByNamespace();
            generator.CreateFiles(sb, ns => $"{ns}.ts");
            generator.WriteFiles();
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
                    var fieldType = GetFieldType(field.fieldType);
                    sb.AppendLine($"    {field} : {fieldType};");
                }
                sb.AppendLine("}");
                return new EmitResult(mapper, sb.ToString());
            }
            if (mapper.type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"type {mapper.type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                return new EmitResult(mapper, sb.ToString());
            }
            return null;
        }
        
        private static string GetFieldType(TypeMapper mapper) {
            var type = mapper.type;
            if (type == typeof(string)) {
                return "string";
            }
            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double)) {
                return "number";
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var elementTypeName = GetFieldType(elementMapper);
                return $"{elementTypeName}[]";
            }
            return type.Name;
        }
    }
}