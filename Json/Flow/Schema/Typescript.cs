// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Utils;
using static Friflo.Json.Flow.Schema.Generator;

namespace Friflo.Json.Flow.Schema
{
    public class Typescript
    {
        public  readonly    Generator                   generator;
        private readonly    Dictionary<TypeDef, string> standardTypes;

        public Typescript (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.schema.StandardTypes);
        }
        
        public void GenerateSchema() {
            var sb = new StringBuilder();
            // emit custom types
            foreach (var type in generator.types) {
                sb.Clear();
                var result = EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPackage(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            EmitPackageHeaders(sb);
            // EmitPackageFooters(sb);  no TS footer
            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}"); // $"{ns.Replace(".", "/")}{generator.extension}");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Unit8,         "uint8 = number" );
            AddType (map, standard.Int16,         "int16 = number" );
            AddType (map, standard.Int32,         "int32 = number" );
            AddType (map, standard.Int64,         "int64 = number" );
               
            AddType (map, standard.Double,        "double = number" );
            AddType (map, standard.Float,         "float = number" );
               
            AddType (map, standard.BigInteger,    "BigInteger = string" );
            AddType (map, standard.DateTime,      "DateTime = string" );
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append("export type ");
            sb.Append(definition);
            sb.AppendLine(";");
            sb.AppendLine();
            return new EmitType(type, generator, sb);
        }
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var standardType    = EmitStandardType(type, sb, generator);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsComplex) {
                var dependencies = new List<TypeDef>();
                var fields          = type.Fields;
                int maxFieldName    = fields.MaxLength(field => field.name.Length);
                
                string  discriminator   = null;
                var     discriminant    = type.Discriminant;
                var     extendsStr      = "";
                if (discriminant != null) {
                    var baseType    = type.BaseType;
                    discriminator   = baseType.UnionType.discriminator;
                    extendsStr = $"extends {baseType.Name} ";
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                    dependencies.Add(baseType);
                } else {
                    var baseType = type.BaseType;
                    if (baseType != null) {
                        extendsStr = $"extends {baseType.Name} ";
                        imports.Add(baseType);
                        dependencies.Add(baseType);
                    }
                }
                var unionType = type.UnionType;
                if (unionType == null) {
                    sb.AppendLine($"export class {type.Name} {extendsStr}{{");
                } else {
                    sb.AppendLine($"export type {type.Name}_Union =");
                    foreach (var polyType in unionType.types) {
                        sb.AppendLine($"    | {polyType.Name}");
                        imports.Add(polyType);
                    }
                    sb.AppendLine($";");
                    sb.AppendLine();
                    sb.AppendLine($"export abstract class {type.Name} {extendsStr}{{");
                    sb.AppendLine($"    abstract {unionType.discriminator}:");
                    foreach (var polyType in unionType.types) {
                        sb.AppendLine($"        | \"{polyType.Discriminant}\"");
                    }
                    sb.AppendLine($"    ;");
                }
                if (discriminant != null) {
                    var indent = Indent(maxFieldName, discriminator);
                    sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
                }
                // fields                
                foreach (var field in fields) {
                    if (type.IsDerivedField(field))
                        continue;
                    bool required = field.required;
                    var fieldType = GetFieldType(field.type, context);
                    var indent  = Indent(maxFieldName, field.name);
                    var optStr  = required ? " ": "?";
                    var nullStr = required ? "" : " | null";
                    sb.AppendLine($"    {field.name}{optStr}{indent} : {fieldType}{nullStr};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, generator, sb, imports, dependencies);
            }
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(type, generator, sb);
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(TypeDef type, TypeContext context) {
            var standard = context.generator.schema.StandardTypes;
            if (type == standard.JsonValue) {
                return "{} | null";
            }
            if (type == standard.String) {
                return "string";
            }
            if (type == standard.Boolean) {
                return "boolean";
            }
            if (type.IsArray) {
                var elementMapper = type.ElementType;
                var elementTypeName = GetFieldType(elementMapper, context);
                return $"{elementTypeName}[]";
            }
            if (type.IsDictionary) {
                var valueMapper = type.ElementType;
                var valueTypeName = GetFieldType(valueMapper, context);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            context.imports.Add(type);
            if (type.UnionType != null)
                return $"{type.Name}_Union";
            return context.generator.GetTypeName(type);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                Package package     = pair.Value;
                string  packageName = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Note}");
                var     max         = package.imports.MaxLength(import => import.Value.package == packageName ? 0 : import.Key.Name.Length);

                foreach (var importPair in package.imports) {
                    var import = importPair.Value;
                    if (import.package == packageName)
                        continue;
                    var typeName = generator.GetTypeName(import.type);
                    var indent = Indent(max, typeName);
                    sb.AppendLine($"import {{ {typeName} }}{indent} from \"./{import.package}\"");
                    if (import.type.UnionType != null) {
                        sb.AppendLine($"import {{ {typeName}_Union }}{indent} from \"./{import.package}\"");
                    }
                }
                package.header = sb.ToString();
            }
        }
    }
}