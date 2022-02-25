// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        
        public static void Generate(Generator generator, string template = null) {
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
            EmitHtmlFile(generator, template, sb);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
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
$@"    <div class='type'>
    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
    </h3>
    <desc>{definition}</desc>
    </div>
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
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                var qualifiedName = type.Namespace + "." + type.Name;
                sb.AppendLine(
$@"    <div class='type'>
    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
        <keyword>enum</keyword>
    </h3>
    <ul class='enum'>");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"        <li>{enumValue}</li>");
                }
                sb.AppendLine(
@"    </ul>
    </div>");
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
            var baseType        = type.BaseType;

            var qualifiedName   = type.Namespace + "." + type.Name;
            var unionType       = type.UnionType;
            var typeName        = type.IsSchema ? "schema": type.IsAbstract ? "abstract class" : "class";
            sb.AppendLine(
$@"    <div class='type'>
    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
        <keyword>{typeName}</keyword>");
            if (baseType != null) {
                var baseName = GetTypeName(baseType, context);
                sb.AppendLine($"        <keyword>extends</keyword> <extends>{baseName}</extends>");
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            sb.AppendLine("    </h3>");

            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (type.IsSchema) {
                sb.AppendLine($"    <chapter>containers</chapter>");    
            }
            sb.AppendLine($"    <table>");
            if (unionType != null) {
                sb.AppendLine(
                    $@"        <tr>
            <td><br><discUnion>{unionType.discriminator}</discUnion></td>
            <td><table>
            <tr><td><keyword>discriminants</keyword></td><td><keyword>sub classes</keyword></td></tr>");
                foreach (var polyType in unionType.types) {
                    var polyTypeDef = polyType.typeDef;
                    var name = GetTypeName (polyTypeDef, context);
                    sb.AppendLine(
                        $@"            <tr>
                <td><discriminant>""{polyType.discriminant}""</discriminant></td>
                <td>{name}</td>
            </tr>");
                    imports.Add(polyTypeDef);
                }
                sb.AppendLine(
@"            </table></td>
        </tr>");
            }
            if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                sb.AppendLine(
$@"        <tr>
            <td><disc>{discriminator}</disc></td>{indent} <td><discriminant>""{discriminant}""</discriminant></td>
        </tr>");
            }
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                var fieldType   = GetFieldType(field, context, field.required);
                var indent      = Indent(maxFieldName, field.name);
                var fieldTag    = "field";
                if (type.KeyField == field.name) {
                    fieldTag    = "key";
                }
                var reference   = "";
                var relation    = field.RelationType;
                if (relation != null) {
                    fieldTag    = "ref";
                    reference   = $"<rel></rel>{GetTypeName(relation, context)}";
                }
                var docs = GetDescription("\n        <tr><td colspan='2'>", field.docs, "</td></tr>");
                // var nullStr = required ? "" : " | null";
                sb.AppendLine(
$@"        <tr>
            <td><{fieldTag}>{field.name}</{fieldTag}></td>{indent} <td>{fieldType}{reference}</td>
        </tr>{docs}");
            }
            sb.AppendLine("    </table>");
            if (type.IsSchema) {
                EmitServiceType(type, context, sb);
            }
            var typeDocs = GetDescription("    <desc>", type.docs, "</desc>");
            if (typeDocs != "")
                sb.AppendLine(typeDocs);   
            sb.AppendLine("    </div>");
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private static void EmitServiceType(TypeDef type, TypeContext context, StringBuilder sb) {
            var commands        = type.Commands;
            sb.AppendLine(
$@"    <br><chapter>commands</chapter>
    <table>
");
            int maxFieldName    = commands.MaxLength(field => field.name.Length);
            foreach (var command in commands) {
                var commandParam    = GetFieldType(command.param,  context, command.param.required);
                var commandResult   = GetFieldType(command.result, context, command.result.required);
                var docs            = GetDescription("\n        <tr><td colspan='2'>", command.docs, "</td></tr>");
                var indent = Indent(maxFieldName, command.name);
                var signature = $"(<keyword>param</keyword>: {commandParam}) : {commandResult}";
                sb.AppendLine(
$@"        <tr>
            <td><cmd>{command.name}</cmd></td>{indent}<td>{signature}</td>
        </tr>{docs}");
            }
            sb.AppendLine("    </table>");
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context, bool required) {
            var nullStr = required ? "" : " | null";
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"{elementTypeName}[]{nullStr}";
            }
            if (field.isDictionary) {
                var keyField = field.type.KeyField;
                var valueTypeName = GetElementType(field, context);
                var key = keyField != null ? $"<key>{keyField}</key>" : "key";
                return $"{key} ➞ {valueTypeName}{nullStr}";
            }
            return GetTypeName(field.type, context) + nullStr;
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
                return "<type>any</type>"; // known as Mr anti-any  :) 
            if (type == standard.String || type == standard.JsonKey)
                return "<type>string</type>";
            if (type == standard.Boolean)
                return "<type>boolean</type>";
            context.imports.Add(type);
            var sb = context.sb;
            sb.Clear();
            sb.Append($"<a href='#{type.Namespace}.{type.Name}'>{type.Name}</a>");
            return sb.ToString();
        }
        
        private static string GetDescription(string prefix, string docs, string suffix) {
            if (docs == null)
                return "";
            return $"{prefix}{docs}{suffix}";
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
        
        private static void EmitHtmlFile(Generator generator, string template, StringBuilder sb) {
            var sbNav = new StringBuilder();
            sb.Clear();
            var fileEmits = OrderNamespaces(generator);
            sbNav.Append("<ul>\n");
            foreach (var emitFile in fileEmits) {
                string ns = emitFile.@namespace;
                // var lastDot = ns.LastIndexOf('.') + 1;
                // var shortNs = lastDot == 0 ? ns : ns.Substring(lastDot); 
                sb.Append(
$@"
<div class='namespace'>
    <h2 id='{ns}'>
        <a href='#{ns}'>{ns}</a>
    </h2>
");
                // sb.AppendLine(emitFile.header);
                sbNav.Append(
$@"    <li><a href='#{ns}'>{ns}</a>
        <ul>
");
                foreach (var result in emitFile.emitTypes) {
                    var type        = result.type;
                    var typeName    = type.Name;
                    var key         = type.KeyField;
                    string tag      = "";
                    if (key != null) {
                        tag         = $"<key style='float:right'>{key}</key>";
                    }
                    if (type.IsSchema) {
                        tag         = $"<keyword style='float:right'>schema</keyword>";
                    }
                    if (type.IsEnum) {
                        tag         = $"<keyword style='float:right'>enum</keyword>";
                    }
                    var disc        = type.Discriminator;
                    var discTag     = disc != null ? $"<disc style='float:right'>{disc}</disc>" : "";
                    var union       = type.UnionType; 
                    if (union != null) {
                        discTag     = $"<discUnion style='float:right'>{union.discriminator}</discUnion>";
                    }
                    sbNav.Append(
$@"            <li><a href='#{ns}.{typeName}'><div>{typeName}{discTag}{tag}</div></a></li>
");
                    sb.Append(result.content);
                }
                sbNav.Append(
@"        </ul>
    </li>
");
                if (emitFile.footer != null)
                    sb.AppendLine(emitFile.footer);
                sb.AppendLine("</div>");
            }
            sbNav.Append("</ul>\n");

            var schemaName      = generator.rootType.Name;
            var htmlFile        = template ?? Template;
            var navigation      = sbNav.ToString();
            var documentation   = sb.ToString();
            htmlFile            = htmlFile.Replace("{{schemaName}}",    schemaName);
            htmlFile            = htmlFile.Replace("{{navigation}}",    navigation);
            htmlFile            = htmlFile.Replace("{{documentation}}", documentation);
            generator.files.Add("schema.html", htmlFile);
        }
        
        private static List<EmitFile> OrderNamespaces(Generator generator) {
            var emitFiles   = new List<EmitFile>(generator.fileEmits.Values);
            var rootType    = generator.rootType;
            emitFiles.Sort((file1, file2) => {
                // namespace Standard to bottom
                if (file1.@namespace == "Standard")
                    return +1;
                if (file2.@namespace == "Standard")
                    return -1;
                // namespace containing root type (schema) on top
                var type1 = file1.emitTypes[0].type; 
                var type2 = file2.emitTypes[0].type; 
                if (type1 == rootType)
                    return -1;
                if (type2 == rootType)
                    return +1;
                // remaining namespace by comparing theirs names
                return string.Compare(file1.@namespace, file2.@namespace, StringComparison.Ordinal);
            });
            return emitFiles;
        }
    }
}