// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;
// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class HtmlGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;

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
            // emitter.EmitFileHeaders(sb);
            // EmitFileFooters(sb);  no TS footer
            EmitHtmlFile(generator, template, sb);
            EmitHtmlMermaidER(generator);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            
            AddType (map, standard.Uint8,       "unsigned integer 8-bit. Range: [0 - 255]" );
            AddType (map, standard.Int16,       "signed integer 16-bit. Range: [-32768, 32767]" );
            AddType (map, standard.Int32,       "signed integer 32-bit. Range: [-2147483648, 2147483647]" );
            AddType (map, standard.Int64,       "signed integer 64-bit. Range: [-9223372036854775808, 9223372036854775807]<br/>" +
                                                "number in JavaScript.  Range: [-9007199254740991, 9007199254740991]");
            
            // NON_CLS
            AddType (map, standard.Int8,        "signed integer 8-bit. Range: [-128 - 127]" );
            AddType (map, standard.UInt16,      "unsigned integer 16-bit. Range: [0, 65535]" );
            AddType (map, standard.UInt32,      "unsigned integer 32-bit. Range: [0, 4294967295]" );
            AddType (map, standard.UInt64,      "unsigned integer 64-bit. Range: [0, 18446744073709551615]<br/>" +
                                                "number in JavaScript.  Range: [0, 9007199254740991]");
               
            AddType (map, standard.Double,      "double precision floating point number" );
            AddType (map, standard.Float,       "single precision floating point number" );
               
            AddType (map, standard.BigInteger,  "integer with arbitrary precision" );
            AddType (map, standard.DateTime,    "timestamp as RFC 3339 + milliseconds" );
            AddType (map, standard.Guid,        "GUID / UUID as RFC 4122. e.g. \"123e4567-e89b-12d3-a456-426614174000\"" );
            AddType (map, standard.JsonTable,   "array of arbitrary values" );
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
                var enumValues      = type.EnumValues;
                var qualifiedName   = type.Namespace + "." + type.Name;
                var doc             = GetDoc("\n    <desc>", type.doc, "</desc>");
                sb.AppendLF(
$@"    <div class='type enum'>
    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
        <keyword>enum</keyword>
    </h3>{doc}
    <table>");
                foreach (var enumValue in enumValues) {
                    var enumDoc = GetDoc("<docs>", enumValue.doc, "</docs>");  
                    sb.AppendLF($"        <tr><td>{enumValue.name}</td><td>{enumDoc}</td></tr>");
                }
                sb.AppendLF(
@"    </table>
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
            var baseType        = type.BaseType;

            var qualifiedName   = type.Namespace + "." + type.Name;
            var unionType       = type.UnionType;
            var typeName        = type.IsSchema ? "schema": type.IsAbstract ? "abstract class" : "class";
            var oasLink         = type.IsSchema ? GetOasLink("/", "open schema API", "") : "";
            var doc             = GetDoc("    <desc>", type.doc, "\n    </desc>");

            sb.AppendLF(
$@"    <div class='type'>
    <h3 id='{qualifiedName}'>
        <a href='#{qualifiedName}'>{type.Name}</a>
        <keyword>{typeName}</keyword>{oasLink}");
            if (baseType != null) {
                var baseName = GetTypeName(baseType, context);
                sb.AppendLF($"        <keyword>extends</keyword> <extends>{baseName}</extends>");
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            sb.AppendLF("    </h3>");
            if (doc != "")
                sb.AppendLF(doc);
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (type.IsSchema) {
                sb.AppendLF($"    <chapter id='containers'><a href='#containers'>containers</a></chapter>");
            }
            sb.AppendLF($"    <table class='fields'>");
            if (unionType != null) {
                sb.AppendLF(
                    $@"        <tr>
            <td><discUnion>{unionType.discriminator}</discUnion></td>
            <td><keyword>discriminator</keyword></td>
            <td><docs>{unionType.doc}</docs>
            <table>");
                foreach (var polyType in unionType.types) {
                    var polyTypeDef = polyType.typeDef;
                    var name = GetTypeName (polyTypeDef, context);
                    sb.AppendLF(
                        $@"            <tr>
                <td><discriminant>""{polyType.discriminant}""</discriminant></td>
                <td>{name}</td>
            </tr>");
                    imports.Add(polyTypeDef);
                }
                sb.AppendLF(
@"            </table></td>
        </tr>");
            }
            if (discriminant != null) {
                var discDoc     = GetDoc("<td><docs>", type.DiscriminatorDoc, "</docs></td>");
                sb.AppendLF(
$@"        <tr>
            <td><disc>{discriminator}</disc></td>
            <td><discriminant>""{discriminant}""</discriminant></td>{discDoc}
        </tr>");
            }
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                var fieldType   = GetFieldType(field, context, field.required);
                var fieldTag    = "field";
                if (type.KeyField?.name == field.name) {
                    fieldTag    = "key";
                }
                var reference   = "";
                var relationType= field.RelationType;
                if (relationType != null) {
                    fieldTag    = "ref";
                    reference   = $"<rel></rel><a href='#{relationType.Namespace}.{relationType.Name}'>{field.relation}</a>";
                }
                var fieldDoc    = GetDoc("\n            <td><docs>", field.doc, "</docs></td>");
                var oasContainer= type.IsSchema ? $"\n            <td>{GetOasLink("/", $"open {field.name} API", field.name)}</td>" : "";
                // var nullStr = required ? "" : " | null";
                sb.AppendLF(
$@"        <tr>
            <td><{fieldTag}>{field.name}</{fieldTag}></td>
            <td><type>{fieldType}{reference}</type></td>{oasContainer}{fieldDoc}
        </tr>");
            }
            sb.AppendLF("    </table>");
            EmitMessages("commands", type.Commands, context, sb);
            EmitMessages("messages", type.Messages, context, sb);
            sb.AppendLF("    </div>");
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private static void EmitMessages(string type, IReadOnlyList<MessageDef> messageDefs, TypeContext context, StringBuilder sb) {
            if (messageDefs == null)
                return;
            var oasCommandsLink = GetOasLink("/commands", "open commands API", "");
            sb.Append(
$@"    <chapter id='{type}'><a href='#{type}'>{type}</a>{oasCommandsLink}</chapter>
    <table class='{type}'>
");
            foreach (var messageDef in messageDefs) {
                var param   = GetMessageArg("param", messageDef.param, context);
                var result  = GetMessageArg(null,    messageDef.result, context);
                var doc     = GetDoc("\n            <td><docs>", messageDef.doc, "</docs></td>");
                var signature = $"({param}) : {result}";
                var path    = messageDef.result != null ? "/commands/post__cmd_" : "/messages/post__msg_";
                var oasLink = GetOasLink(path, $"open {messageDef.name} API", messageDef.name);
                sb.AppendLF(
$@"        <tr>
            <td><cmd>{messageDef.name}</cmd></td>
            <td><sig>{signature}</sig></td>
            <td>{oasLink}</td>{doc}
        </tr>");
            }
            sb.AppendLF("    </table>");
        }
        
        private static string GetMessageArg(string name, FieldDef fieldDef, TypeContext context) {
            if (fieldDef == null)
                return name != null ? "" : "void";
            var argType = GetFieldType(fieldDef, context, fieldDef.required);
            return name != null ? $"<keyword>{name}</keyword>: {argType}" : argType;
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
                return "<predef>any</predef>"; // known as Mr anti-any  :) 
            if (type == standard.JsonEntity)
                return "<predef>any</predef>"; 
            if (type == standard.String || type == standard.ShortString)
                return "<predef>string</predef>";
            if (type == standard.JsonKey)
                return "<predef>(string | integer)</predef>";
            if (type == standard.Boolean)
                return "<predef>boolean</predef>";
            context.imports.Add(type);
            var sb = context.sb;
            sb.Clear();
            sb.Append($"<a href='#{type.Namespace}.{type.Name}'>{type.Name}</a>");
            return sb.ToString();
        }
        
        private static string GetDoc(string prefix, string docs, string suffix) {
            if (docs == null)
                return "";
            return $"{prefix}{docs}{suffix}";
        }

        private static string GetOasLink(string tag, string description, string local) {
            local = local.Replace(".", "_");
            return $"<oas><a href='../open-api.html#{tag}{local}' target='_blank' title='{description} as OpenAPI specification (OAS) in new tab'>OAS</a></oas>";
        }
        
        private static void EmitHtmlFile(Generator generator, string template, StringBuilder sb) {
            var sbNav = new StringBuilder();
            sb.Clear();
            var fileEmits = generator.OrderNamespaces();
            sbNav.Append("<ul>\n");
            foreach (var emitFile in fileEmits) {
                string ns = emitFile.@namespace;
                // var lastDot = ns.LastIndexOf('.') + 1;
                // var shortNs = lastDot == 0 ? ns : ns.Substring(lastDot); 
                sb.Append(
$@"
<div class='namespace'>
    <h2 id='{ns}'>
        <a href='#{ns}'>{ns}</a> <keyword>namespace</keyword>
    </h2>
");
                // sb.AppendLF(emitFile.header);
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
                        tag         = $"<key>{key}</key>";
                    }
                    if (type.IsSchema) {
                        tag         = $"<keyword>schema</keyword>";
                    }
                    if (type.IsEnum) {
                        tag         = $"<keyword>enum</keyword>";
                    }
                    var disc        = type.Discriminator;
                    var discTag     = disc != null ? $"<disc>{disc}</disc>" : "";
                    var union       = type.UnionType; 
                    if (union != null) {
                        discTag     = $"<discUnion>{union.discriminator}</discUnion>";
                    }
                    sbNav.Append(
$@"            <li><a href='#{ns}.{typeName}'><div><span>{typeName}</span>{discTag}{tag}</div></a></li>
");
                    sb.Append(result.content);
                }
                sbNav.Append(
@"        </ul>
    </li>
");
                if (emitFile.footer != null)
                    sb.AppendLF(emitFile.footer);
                sb.AppendLF("</div>");
            }
            sbNav.Append("</ul>\n");

            var schemaName      = generator.rootType.Name;
            var htmlFile        = template ?? Template;
            var navigation      = sbNav.ToString();
            var documentation   = sb.ToString();
            htmlFile            = htmlFile.Replace("{{schemaName}}",        schemaName);
            htmlFile            = htmlFile.Replace("{{documentation}}",     documentation);
            htmlFile            = htmlFile.Replace("{{navigation}}",        navigation);
            htmlFile            = htmlFile.Replace("{{generatedByLink}}",   Link);
            generator.files.Add("schema.html", htmlFile);
        }
        
        private static void EmitHtmlMermaidER(Generator generator) {
            var mermaidGenerator    = new Generator(generator.typeSchema, ".mmd");
            var schemaName          = mermaidGenerator.rootType.Name;
            var htmlFile            = Mermaid.Replace("SCHEMA_NAME", schemaName);             
            MermaidClassDiagramGenerator.Generate(mermaidGenerator);
            var mermaidFile         = mermaidGenerator.files["class-diagram.mmd"];
            
            htmlFile                = htmlFile.Replace("{{schemaName}}",            schemaName);
            htmlFile                = htmlFile.Replace("{{mermaidClassDiagram}}",   mermaidFile);
            htmlFile                = htmlFile.Replace("{{generatedByLink}}",       Link);
            
            generator.files.Add("class-diagram.html", htmlFile);
        }
    }
}