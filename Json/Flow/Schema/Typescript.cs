// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Utils;
using Friflo.Json.Flow.Schema.Utils.Mapper;

using static Friflo.Json.Flow.Schema.Generator;

namespace Friflo.Json.Flow.Schema
{
    public class Typescript
    {
        public  readonly    Generator                   generator;
        private readonly    Dictionary<ITyp, string>    standardTypes;

        public Typescript (TypeStore typeStore, ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            var system      = new NativeTypeSystem(typeStore.GetTypeMappers());
            var sepTypes    = system.GetTypes(separateTypes);
            generator       = new Generator(system, stripNamespaces, ".ts", sepTypes);
            standardTypes   = GetStandardTypes(generator.system);
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
        
        private static Dictionary<ITyp, string> GetStandardTypes(ITypeSystem system) {
            var map = new Dictionary<ITyp, string>();
            AddType(map, system.Unit8,         "uint8 = number" );
            AddType(map, system.Int16,         "int16 = number" );
            AddType(map, system.Int32,         "int32 = number" );
            AddType(map, system.Int64,         "int64 = number" );
               
            AddType(map, system.Double,        "double = number" );
            AddType(map, system.Float,         "float = number" );
               
            AddType(map, system.BigInteger,    "BigInteger = string" );
            AddType(map, system.DateTime,      "DateTime = string" );
            return map;
        }

        private EmitType EmitStandardType(ITyp type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append("export type ");
            sb.Append(definition);
            sb.AppendLine(";");
            sb.AppendLine();
            return new EmitType(type, TypeSemantic.None, generator, sb);
        }
        
        private EmitType EmitType(ITyp type, StringBuilder sb) {
            var semantic= type.TypeSemantic;
            var imports = new HashSet<ITyp>();
            var context = new TypeContext (generator, imports, type);
            // mapper      = mapper.GetUnderlyingMapper();
            // var type    = Generator.GetType(mapper);
            var standardType = EmitStandardType(type, sb, generator);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsComplex) {
                var dependencies = new List<ITyp>();
                var fields          = type.Fields;
                int maxFieldName    = fields.MaxLength(field => field.jsonName.Length);
                
                string  discriminator = null;
                var     discriminant = type.Discriminant;
                var extendsStr = "";
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
                var instanceFactory = type.UnionType;
                if (instanceFactory == null) {
                    sb.AppendLine($"export class {type.Name} {extendsStr}{{");
                } else {
                    sb.AppendLine($"export type {type.Name}_Union =");
                    foreach (var polyType in instanceFactory.polyTypes) {
                        sb.AppendLine($"    | {polyType.Name}");
                        imports.Add(polyType);
                    }
                    sb.AppendLine($";");
                    sb.AppendLine();
                    sb.AppendLine($"export abstract class {type.Name} {extendsStr}{{");
                    sb.AppendLine($"    abstract {instanceFactory.discriminator}:");
                    foreach (var polyType in instanceFactory.polyTypes) {
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
                    bool isOptional = !field.required;
                    var fieldType = GetFieldType(field.fieldType, context, ref isOptional);
                    var indent = Indent(maxFieldName, field.jsonName);
                    var optStr = isOptional ? "?" : " ";
                    var nullStr = isOptional ? " | null" : "";
                    sb.AppendLine($"    {field.jsonName}{optStr}{indent} : {fieldType}{nullStr};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, imports, dependencies);
            }
            if (type.IsEnum) {
                var enumValues = type.GetEnumValues();
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
        
        private string GetFieldType(ITyp type, TypeContext context, ref bool isOptional) {
            var system  = context.generator.system;
            isOptional  = isOptional && type.IsNullable;
            // mapper      = mapper.GetUnderlyingMapper();
            // var type    = Generator.GetType(mapper);
            if (type == system.JsonValue) {
                return "{} | null";
            }
            if (type == system.String) {
                return "string";
            }
            if (type == system.Boolean) {
                return "boolean";
            }
            if (type.IsArray) {
                var elementMapper = type.ElementType;
                var isOpt = false;
                var elementTypeName = GetFieldType(elementMapper, context, ref isOpt);
                return $"{elementTypeName}[]";
            }
            var isDictionary = type.IsDictionary;
            if (isDictionary) {
                var valueMapper = type.ElementType;
                var isOpt = false;
                var valueTypeName = GetFieldType(valueMapper, context, ref isOpt);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            context.imports.Add(type);
            if (type.UnionType != null)
                return $"{type.Name}_Union";
            return generator.GetTypeName(type);
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