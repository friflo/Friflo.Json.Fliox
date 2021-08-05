// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Utils;
using static Friflo.Json.Flow.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Flow.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Flow.Schema
{
    public partial class TypescriptGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Union = "_Union";

        private TypescriptGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new TypescriptGenerator(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPath(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            emitter.EmitFileHeaders(sb);
            // EmitFileFooters(sb);  no TS footer
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Uint8,         "uint8 = number" );
            AddType (map, standard.Int16,         "int16 = number" );
            AddType (map, standard.Int32,         "int32 = number" );
            AddType (map, standard.Int64,         "int64 = number" );
               
            AddType (map, standard.Double,        "double = number" );
            AddType (map, standard.Float,         "float = number" );
               
            AddType (map, standard.BigInteger,    "BigInteger = string" );
            AddType (map, standard.DateTime,      "DateTime = string" );
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append("export type ");
            sb.Append(definition);
            sb.AppendLine(";");
            sb.AppendLine();
            return new EmitType(type, sb);
        }
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            var standardType    = EmitStandardType(type, sb);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsClass) {
                return EmitClassType(type, sb);
            }
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var dependencies    = new List<TypeDef>();
            var fields          = type.Fields;
            int maxFieldName    = fields.MaxLength(field => field.name.Length);
            var extendsStr      = "";
            var baseType        = type.BaseType;
            if (baseType != null) {
                extendsStr = $"extends {baseType.Name} ";
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            var unionType = type.UnionType;
            if (unionType == null) {
                var abstractStr = type.IsAbstract ? "abstract " : "";
                sb.AppendLine($"export {abstractStr}class {type.Name} {extendsStr}{{");
            } else {
                sb.AppendLine($"export type {type.Name}{Union} =");
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
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
            }
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                bool required = field.required;
                var fieldType = GetFieldType(field, context);
                var indent  = Indent(maxFieldName, field.name);
                var optStr  = required ? " ": "?";
                var nullStr = required ? "" : " | null";
                sb.AppendLine($"    {field.name}{optStr}{indent} : {fieldType}{nullStr};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context) {
            var type = field.type;
            if (field.isArray) {
                var elementTypeName = GetTypeName(type, context);
                return $"{elementTypeName}[]";
            }
            if (field.isDictionary) {
                var valueTypeName = GetTypeName(type, context);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            return GetTypeName(type, context);
        }
        
        private static string GetTypeName(TypeDef type, TypeContext context) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return "any"; // known as Mr anti-any  :) 
            if (type == standard.String)
                return "string";
            if (type == standard.Boolean)
                return "boolean";
            context.imports.Add(type);
            if (type.UnionType != null)
                return $"{type.Name}{Union}";
            return type.Name;
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                string      filePath    = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Note}");
                var max = emitFile.imports.MaxLength(imp => {
                    var typeDef = imp.Value.type;
                    var len = typeDef.UnionType != null ? typeDef.Name.Length + Union.Length : typeDef.Name.Length;
                    return typeDef.Path == filePath ? 0 : len;
                });
                foreach (var importPair in emitFile.imports) {
                    var import = importPair.Value.type;
                    if (import.Path == filePath)
                        continue;
                    var typeName    = import.Name;
                    var indent      = Indent(max, typeName);
                    sb.AppendLine($"import {{ {typeName} }}{indent} from \"./{import.Path}\"");
                    if (import.UnionType != null) {
                        var unionName = $"{typeName}{Union}";
                        indent      = Indent(max, unionName);
                        sb.AppendLine($"import {{ {unionName} }}{indent} from \"./{import.Path}\"");
                    }
                }
                emitFile.header = sb.ToString();
            }
        }
    }
}