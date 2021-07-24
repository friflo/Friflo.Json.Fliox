// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Flow.Schema
{
    public class Typescript
    {
        public  readonly    Generator  generator;

        public Typescript (TypeStore typeStore, ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            generator = new Generator(typeStore, stripNamespaces, ".ts", separateTypes);
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
            generator.GroupTypesByPackage(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            EmitPackageHeaders(sb);
            // EmitPackageFooters(sb);  no TS footer
            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}"); // $"{ns.Replace(".", "/")}{generator.extension}");
        }
        
        private readonly Dictionary<Type, string> standardTypes = new Dictionary<Type, string> {
            { typeof(byte),         "uint8 = number" },
            { typeof(short),        "int16 = number" },
            { typeof(int),          "int32 = number" },
            { typeof(long),         "int64 = number" },
            
            { typeof(double),       "double = number" },
            { typeof(float),        "float = number" },
            
            { typeof(BigInteger),   "BigInteger = string" },
            { typeof(DateTime),     "DateTime = string" }
        }; 

        private EmitType EmitStandardType(Type type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append("export type ");
            sb.Append(definition);
            sb.AppendLine(";");
            sb.AppendLine();
            return new EmitType(type, TypeSemantic.None, generator, sb);
        }
        
        private EmitType EmitType(TypeMapper mapper, StringBuilder sb) {
            var semantic= mapper.GetTypeSemantic();
            var imports = new HashSet<Type>();
            var context = new TypeContext (generator, imports, mapper);
            mapper      = mapper.GetUnderlyingMapper();
            var type    = Generator.GetType(mapper);
            var standardType = EmitStandardType(type, sb, generator);
            if (standardType != null ) {
                return standardType;
            }
            if (mapper.IsComplex) {
                var dependencies = new List<Type>();
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.MaxLength(field => field.jsonName.Length);
                
                string  discriminator = null;
                var     discriminant = mapper.Discriminant;
                var extendsStr = "";
                if (discriminant != null) {
                    var baseMapper  = generator.GetBaseMapper(type);
                    discriminator   = baseMapper.InstanceFactory.discriminator;
                    extendsStr = $"extends {baseMapper.type.Name} ";
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                    dependencies.Add(baseMapper.type);
                } else {
                    var baseMapper = generator.GetBaseMapper(type);
                    if (baseMapper != null) {
                        extendsStr = $"extends {baseMapper.type.Name} ";
                        imports.Add(baseMapper.type);
                        dependencies.Add(baseMapper.type);
                    }
                }
                var instanceFactory = mapper.InstanceFactory;
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
                    bool isOptional = !field.required;
                    var fieldType = GetFieldType(field.fieldType, context, ref isOptional);
                    var indent = Generator.Indent(maxFieldName, field.jsonName);
                    var optStr = isOptional ? "?" : " ";
                    var nullStr = isOptional ? " | null" : "";
                    sb.AppendLine($"    {field.jsonName}{optStr}{indent} : {fieldType}{nullStr};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, imports, dependencies);
            }
            if (type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb);
            }
            return null;
        }
        
        private string GetFieldType(TypeMapper mapper, TypeContext context, ref bool isOptional) {
            mapper      = mapper.GetUnderlyingMapper();
            isOptional  = isOptional && mapper.isNullable;
            var type    = Generator.GetType(mapper);
            if (type == typeof(JsonValue)) {
                return "{} | null";
            }
            if (type == typeof(string)) {
                return "string";
            }
            if (type == typeof(bool)) {
                return "boolean";
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var isOpt = false;
                var elementTypeName = GetFieldType(elementMapper, context, ref isOpt);
                return $"{elementTypeName}[]";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var isOpt = false;
                var valueTypeName = GetFieldType(valueMapper, context, ref isOpt);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            context.imports.Add(type);
            if (context.generator.IsUnionType(type))
                return $"{type.Name}_Union";
            return generator.GetTypeName(type);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                Package package     = pair.Value;
                string  packageName = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Generator.Note}");
                var     max         = package.imports.MaxLength(import => import.Value.package == packageName ? 0 : import.Key.Name.Length);

                foreach (var importPair in package.imports) {
                    var import = importPair.Value;
                    if (import.package == packageName)
                        continue;
                    var typeName = generator.GetTypeName(import.type);
                    var indent = Generator.Indent(max, typeName);
                    sb.AppendLine($"import {{ {typeName} }}{indent} from \"./{import.package}\"");
                    if (generator.IsUnionType(import.type)) {
                        sb.AppendLine($"import {{ {typeName}_Union }}{indent} from \"./{import.package}\"");
                    }
                }
                package.header = sb.ToString();
            }
        }
    }
}