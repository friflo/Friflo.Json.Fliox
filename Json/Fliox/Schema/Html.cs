// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Fliox.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Fliox.Schema
{
    public sealed partial class HtmlGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Union = "_Union";

        private HtmlGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new HtmlGenerator(generator);
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
            EmitHtmlFile(generator, sb);
        }
        
        private static void EmitHtmlFile(Generator generator, StringBuilder sb) {
            var sbNav = new StringBuilder();
            sb.Clear();
            sbNav.Append("<ul>\n");
            foreach (var pair in generator.fileEmits) {
                string      ns          = pair.Key;
                EmitFile    emitFile    = pair.Value;
                sb.Append(
$@"
<p>
    <h2 id='{ns}'>
        <a href='#{ns}'>{ns}</a>
    <h2>
");
                // sb.AppendLine(emitFile.header);
                sbNav.Append(
$@"    <li><a href='#{ns}'>{ns}</a></li>
        <ul>
");
                foreach (var result in emitFile.emitTypes) {
                    var typeName = result.type.Name;
                    sbNav.Append(
$@"            <li><a href='#{ns}.{typeName}'>{typeName}</a></li>
");
                    sb.Append(result.content);
                }
                sbNav.Append(
@"        </ul>
    </li>
");
                if (emitFile.footer != null)
                    sb.AppendLine(emitFile.footer);
                sb.AppendLine("</p>");
            }
            sbNav.Append("</ul>\n");

            var schemaName      = generator.rootType.Name;
            var htmlFile        = GetTemplate();
            var navigation      = sbNav.ToString();
            var documentation   = sb.ToString();
            htmlFile            = htmlFile.Replace("{{schemaName}}",    schemaName);
            htmlFile            = htmlFile.Replace("{{navigation}}",    navigation);
            htmlFile            = htmlFile.Replace("{{documentation}}", documentation);
            generator.files.Add("schema.html", htmlFile);
        }
        
        private static string GetTemplate() {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("Friflo.Json.Fliox.Schema.html-template.html"))
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            var nl= Environment.NewLine;
            AddType (map, standard.Uint8,       $"unsigned integer 8-bit. Range: [0 - 255]" );
            AddType (map, standard.Int16,       $"signed integer 16-bit. Range: [-32768, 32767]" );
            AddType (map, standard.Int32,       $"signed integer 32-bit. Range: [-2147483648, 2147483647]" );
            AddType (map, standard.Int64,       $"signed integer 64-bit. Range: [-9223372036854775808, 9223372036854775807]" );
               
            AddType (map, standard.Double,      $"double precision floating point number" );
            AddType (map, standard.Float,       $"single precision floating point number" );
               
            AddType (map, standard.BigInteger,  $"integer with arbitrary precision" );
            AddType (map, standard.DateTime,    $"timestamp as RFC 3339 + milliseconds" );
            AddType (map, standard.Guid,        $"GUID / UUID as RFC 4122. e.g. \"123e4567-e89b-12d3-a456-426614174000\"" );
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            var qualifiedName = type.Namespace + "." + type.Name;
            sb.Append(
$@"    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
    </h3>
    <div>{definition}</div>
");
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
            if (type.IsService) {
                return EmitServiceType(type, sb);
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
                extendsStr = $" extends {baseType.Name}";
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            var qualifiedName = type.Namespace + "." + type.Name;
            var unionType = type.UnionType;
            if (unionType == null) {
                var abstractStr = type.IsAbstract ? "abstract " : "";
                sb.AppendLine(
$@"    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{abstractStr}class {type.Name}{extendsStr}</a>");
            } else {
                sb.AppendLine($"export type {type.Name}{Union} =");
                foreach (var polyType in unionType.types) {
                    var polyTypeDef = polyType.typeDef;
                    sb.AppendLine($"    | {polyTypeDef.Name}");
                    imports.Add(polyTypeDef);
                }
                sb.AppendLine($";");
                sb.AppendLine();
                sb.AppendLine($"export abstract class {type.Name}{extendsStr}{{");
                sb.AppendLine($"    abstract {unionType.discriminator}:");
                foreach (var polyType in unionType.types) {
                    sb.AppendLine($"        | \"{polyType.discriminant}\"");
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
            sb.AppendLine("    </h3>");
            sb.AppendLine($"    <table  class='type'");
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                bool required = field.required;
                var fieldType = GetFieldType(field, context);
                var indent  = Indent(maxFieldName, field.name);
                var optStr  = required ? "": "?";
                var nullStr = required ? "" : " | null";
                sb.AppendLine(
$@"        <tr>
            <td>{field.name}{optStr}</td>{indent} <td>{fieldType}{nullStr}</td>
        </tr>");
            }
            sb.AppendLine($"    </table>");
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private EmitType EmitServiceType(TypeDef type, StringBuilder sb) {
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var dependencies    = new List<TypeDef>();
            var commands        = type.Commands;
            var qualifiedName   = type.Namespace + "." + type.Name;
            sb.AppendLine(
$@"    <h3 id={qualifiedName}>
        <a href='#{qualifiedName}'>interface {type.Name}</a>
    </h3>
    <table>
");
            int maxFieldName    = commands.MaxLength(field => field.name.Length);
            foreach (var command in type.Commands) {
                var commandParam    = GetTypeName(command.param,  context);
                var commandResult   = GetTypeName(command.result, context);
                var indent = Indent(maxFieldName, command.name);
                var signature = $"(param: {commandParam}) : {commandResult}";
                sb.AppendLine(
$@"        <tr>
            <td>{command.name}</td>{indent}<td>{signature}</td>
        </tr>");
            }
            sb.AppendLine(
"    </table>");
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"{elementTypeName}[]";
            }
            if (field.isDictionary) {
                var valueTypeName = GetElementType(field, context);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            return GetTypeName(field.type, context);
        }
        
        private static string GetElementType(FieldDef field, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, context);
            if (field.isNullableElement)
                return $"({elementTypeName} | null)";
            return elementTypeName;
        }
        
        private static string GetTypeName(TypeDef type, TypeContext context) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return "any"; // known as Mr anti-any  :) 
            if (type == standard.String || type == standard.JsonKey)
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