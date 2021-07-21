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
    public class JsonSchema
    {
        private readonly    Generator   generator;
        private const       string      Next = ",\r\n";
        

        public JsonSchema (Generator generator) {
            this.generator = generator;
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
            sb.AppendLine("}");
            generator.GroupTypesByNamespace();
            EmitPackageHeaders(sb);
            EmitPackageFooters(sb);

            generator.CreateFiles(sb, ns => $"{ns}.json", Next); // $"{ns.Replace(".", "/")}.ts");
        }
        
        private EmitType EmitType(TypeMapper mapper, StringBuilder sb) {
            var imports = new HashSet<Type>(); 
            mapper      = Generator.GetUnderlyingTypeMapper(mapper);
            var type                = mapper.type;
            if (mapper.IsComplex) {
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.MaxLength(field => field.name.Length);
                
                string  discriminator = null;
                var     discriminant = mapper.discriminant;
                if (discriminant != null) {
                    var baseMapper  = generator.GetPolymorphBaseMapper(type);
                    discriminator   = baseMapper.instanceFactory.discriminator;
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var instanceFactory = mapper.instanceFactory;
                sb.AppendLine($"        \"{type.Name}\": {{");
                if (instanceFactory == null) {
                    sb.AppendLine($"            \"type\": \"object\",");
                } else {
                    sb.AppendLine($"            \"oneOf\": [");
                    bool firstElem = true;
                    foreach (var polyType in instanceFactory.polyTypes) {
                        Generator.Delimiter(sb, Next, ref firstElem);
                        sb.Append($"                {{ {Ref(polyType.type, mapper)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"            ],");
                }
                sb.AppendLine($"            \"properties\": {{");
                bool firstField = true;
                var required = new List<string>();
                if (discriminant != null) {
                    var indent = Generator.Indent(maxFieldName, discriminator);
                    sb.Append($"                \"{discriminator}\":{indent} {{ \"enum\": [\"{discriminant}\"] }}");
                    firstField = false;
                    required.Add(discriminator);
                }
                // fields
                foreach (var field in fields) {
                    if (generator.IsDerivedField(type, field))
                        continue;
                    var context = new FieldContext (imports, mapper, mapper.GetTypeSemantic());
                    var fieldType = GetFieldType(field.fieldType, context, out var isOptional);
                    var indent = Generator.Indent(maxFieldName, field.name);
                    if (field.required || !isOptional)
                        required.Add(field.name);
                    Generator.Delimiter(sb, Next, ref firstField);
                    sb.Append($"                \"{field.name}\":{indent} {{ {fieldType} }}");
                }
                sb.AppendLine();
                sb.AppendLine("            },");
                if (required.Count > 0 ) {
                    bool firstReq = true;
                    sb.AppendLine("            \"required\": [");
                    foreach (var item in required) {
                        Generator.Delimiter(sb, Next, ref firstReq);
                        sb.Append ($"                \"{item}\"");
                    }
                    sb.AppendLine();
                    sb.AppendLine("            ],");
                }
                sb.AppendLine("            \"additionalProperties\": false");
                sb.Append    ("        }");
                return new EmitType(mapper, sb.ToString(), imports);
            }
            if (type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"        \"{type.Name}\": {{");
                sb.AppendLine($"            \"enum\": [");
                bool firstValue = true;
                foreach (var enumValue in enumValues) {
                    Generator.Delimiter(sb, Next, ref firstValue);
                    sb.Append($"                \"{enumValue}\"");
                }
                sb.AppendLine();
                sb.AppendLine("            ]");
                sb.Append    ("        }");
                return new EmitType(mapper, sb.ToString(), new HashSet<Type>());
            }
            return null;
        }
        
        private string GetFieldType(TypeMapper mapper, FieldContext context, out bool isOptional) {
            mapper = Generator.GetUnderlyingFieldMapper(mapper);
            var type = mapper.type;
            isOptional = true;
            if (type == typeof(JsonValue)) {
                return "\"type\": \"object\"";
            }
            if (type == typeof(string)) {
                return "\"type\": \"string\"";
            }
            if (mapper.isValueType) { 
                isOptional = mapper.isNullable;
                if (isOptional) {
                    type = mapper.nullableUnderlyingType;
                }
                if (type == typeof(bool)) {
                    return "\"type\": \"boolean\"";
                }
                if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                    || type == typeof(float) || type == typeof(double)) {
                    return "\"type\": \"number\"";
                }
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var elementTypeName = GetFieldType(elementMapper, context, out isOptional);
                return $"\"type\": \"array\", \"items\": {{ {elementTypeName} }}";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var valueTypeName = GetFieldType(valueMapper, context, out isOptional);
                return $"\"type\": \"object\", \"additionalProperties\": {{ {valueTypeName} }}";
            }
            context.imports.Add(type);
            return Ref(type, context.owner);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine("{");
                sb.AppendLine("    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLine("    \"$comment\": \"Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema\",");
                sb.Append    ("    \"definitions\": {");
                package.header = sb.ToString();
            }
        }
        
        private void EmitPackageFooters(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");
                package.footer = sb.ToString();
            }
        }
        
        private static string Ref(Type type, TypeMapper owner) {
            var name = type.Name;
            // if (generator.IsUnionType(type))
            //    name = $"{type.Name}_Union";
            bool samePackage = type.Namespace == owner.type.Namespace;
            var prefix = samePackage ? "" : $"./{type.Namespace}.json";
            return $"\"$ref\": \"{prefix}#/definitions/{name}\"";
        }
    }
}