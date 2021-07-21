// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Flow.Schema
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
            generator.GroupTypesByPackage();
            EmitPackageHeaders(sb);
            generator.CreateFiles(sb, ns => $"{ns}.ts"); // $"{ns.Replace(".", "/")}.ts");
        }
        
        private EmitType EmitType(TypeMapper mapper, StringBuilder sb) {
            var imports = new HashSet<Type>();
            var context = new TypeContext (generator, imports, mapper, mapper.GetTypeSemantic());
            mapper      = Generator.GetUnderlyingTypeMapper(mapper);
            var type    = mapper.type;
            if (mapper.IsComplex) {
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.MaxLength(field => field.name.Length);
                
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
                    sb.AppendLine($"export type {type.Name}_Union =");
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
                    sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
                }
                
                // fields                
                foreach (var field in fields) {
                    if (generator.IsDerivedField(type, field))
                        continue;
                    var fieldType = GetFieldType(field.fieldType, context, out var isOptional);
                    var indent = Generator.Indent(maxFieldName, field.name);
                    var optStr = field.required || !isOptional ? " " : "?";
                    sb.AppendLine($"    {field.name}{optStr}{indent} : {fieldType};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(mapper, null, sb.ToString(), imports);
            }
            if (type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(mapper, null, sb.ToString(), new HashSet<Type>());
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(TypeMapper mapper, TypeContext context, out bool isOptional) {
            mapper = Generator.GetUnderlyingFieldMapper(mapper);
            var type = mapper.type;
            isOptional = true;
            if (type == typeof(JsonValue)) {
                return "object";
            }
            if (type == typeof(string)) {
                return "string";
            }
            if (mapper.isValueType) { 
                isOptional = mapper.isNullable;
                if (isOptional) {
                    type = mapper.nullableUnderlyingType;
                }
                if (type == typeof(bool)) {
                    return "boolean";
                }
                if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                    || type == typeof(float) || type == typeof(double)) {
                    return "number";
                }
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var elementTypeName = GetFieldType(elementMapper, context, out isOptional);
                return $"{elementTypeName}[]";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var valueTypeName = GetFieldType(valueMapper, context, out isOptional);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            context.imports.Add(type);
            if (context.generator.IsUnionType(type))
                return $"{type.Name}_Union";
            return type.Name;
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                sb.Clear();
                sb.AppendLine("// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema");
                string  ns      = pair.Key;
                Package package = pair.Value;
                var     max     = package.imports.MaxLength(p => p.Key.Namespace == ns ? 0 : p.Key.Name.Length);

                foreach (var importPair in package.imports) {
                    var import = importPair.Key;
                    if (import.Namespace == ns)
                        continue;
                    var typeName = import.Name;
                    var indent = Generator.Indent(max, typeName);
                    sb.AppendLine($"import {{ {typeName} }}{indent} from \"./{import.Namespace}\"");
                    if (generator.IsUnionType(import)) {
                        sb.AppendLine($"import {{ {typeName}_Union }}{indent} from \"./{import.Namespace}\"");
                    }
                }
                package.header = sb.ToString();
            }
        }
    }
}