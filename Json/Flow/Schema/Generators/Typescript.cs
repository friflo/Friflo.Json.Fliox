// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.Generators
{
    public class Typescript
    {
        private readonly    Generator   generator;

        public Typescript (Generator generator) {
            this.generator  = generator;
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
            EmitHeader(sb);
            generator.CreateFiles(sb, ns => $"{ns}.ts");
            
            // generator.CreateFiles(sb, ns => $"{ns.Replace(".", "/")}.ts");
            generator.WriteFiles();
        }
        
        private EmitResult EmitType(TypeMapper mapper, StringBuilder sb) {
            var imports             = new HashSet<Type>(); 
            var underlyingMapper    = mapper.GetUnderlyingMapper();
            var type                = mapper.type;
            if (underlyingMapper != null) {
                mapper = underlyingMapper;
            }
            if (mapper.IsComplex) {
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.Length > 0 ? fields.Max(field => field.name.Length) : 0;
                
                string  discriminator = null;
                var     discriminant = mapper.discriminant;
                var extendsStr = "";
                if (discriminant != null) {
                    var baseMapper  = generator.GetPolymorphBaseMapper(type);
                    discriminator   = baseMapper.instanceFactory.discriminator;
                    extendsStr = $"extends {baseMapper.type.Name} ";
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory == null) {
                    sb.AppendLine($"export class {type.Name} {extendsStr}{{");
                } else {
                    sb.AppendLine($"type {type.Name}_Union =");
                    foreach (var polyType in instanceFactory.polyTypes) {
                        sb.AppendLine($"    | {polyType.type.Name}");
                        imports.Add(polyType.type);
                    }
                    sb.AppendLine($";");
                    sb.AppendLine();
                    sb.AppendLine($"export abstract class {type.Name} {extendsStr}{{");
                    sb.AppendLine($"    abstract {instanceFactory.discriminator}:");
                    foreach (var polyType in instanceFactory.polyTypes) {
                        sb.AppendLine($"        | \"{polyType.name}\"");
                    }
                    sb.AppendLine($"    ;");
                }
                if (discriminant != null) {
                    var indent = Generator.Indent(maxFieldName, discriminator);
                    sb.AppendLine($"    {discriminator}:{indent} \"{discriminant}\";");
                }
                
                // fields                
                foreach (var field in fields) {
                    var fieldType = GetFieldType(field.fieldType, imports);
                    var indent = Generator.Indent(maxFieldName, field.name);
                    sb.AppendLine($"    {field.name}:{indent} {fieldType};");
                }
                sb.AppendLine("}");
                return new EmitResult(mapper, sb.ToString(), imports);
            }
            if (type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                return new EmitResult(mapper, sb.ToString(), new HashSet<Type>());
            }
            return null;
        }
        
        private static string GetFieldType(TypeMapper mapper, HashSet<Type> imports) {
            var type = mapper.type;
            if (type == typeof(JsonValue)) {
                return "object";
            }
            if (mapper.isValueType && mapper.isNullable) {
                type = mapper.nullableUnderlyingType;
            }
            if (type == typeof(string)) {
                return "string";
            }
            if (type == typeof(bool)) {
                return "boolean";
            }
            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double)) {
                return "number";
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var elementTypeName = GetFieldType(elementMapper, imports);
                return $"{elementTypeName}[]";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var valueTypeName = GetFieldType(valueMapper, imports);
                return $"{{ string: {valueTypeName} }}";
            }
            imports.Add(type);
            return type.Name;
        }
        
        private void EmitHeader(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                sb.Clear();
                string      ns      = pair.Key;
                Package     package = pair.Value;
                var max = 0;
                if (package.imports.Count > 0) {
                    max = package.imports.Max(import => import.Namespace == ns ? 0 : import.Name.Length);
                }
                foreach (var import in package.imports) {
                    if (import.Namespace == ns)
                        continue;
                    var indent = Generator.Indent(max, import.Name);
                    sb.AppendLine($"import {{ {import.Name} }}{indent} from \"./{import.Namespace}\"");
                }
                package.header = sb.ToString();
            }
        }
    }
}