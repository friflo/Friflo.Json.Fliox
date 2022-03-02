// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Fliox.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Fliox.Schema
{
    public sealed partial class JsonSchemaGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Next = ",\r\n";
        
        private JsonSchemaGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new JsonSchemaGenerator(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPath(false);
            emitter.EmitFileHeaders(sb);
            emitter.EmitFileFooters(sb);
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}", Next);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Uint8,       "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 255" );
            AddType (map, standard.Int16,       "\"type\": \"integer\", \"minimum\": -32768, \"maximum\": 32767" );
            AddType (map, standard.Int32,       "\"type\": \"integer\", \"minimum\": -2147483648, \"maximum\": 2147483647" );
            AddType (map, standard.Int64,       "\"type\": \"integer\", \"minimum\": -9223372036854775808, \"maximum\": 9223372036854775807" );
                
            AddType (map, standard.Double,      "\"type\": \"number\"" );
            AddType (map, standard.Float,       "\"type\": \"number\"" );
                
            AddType (map, standard.BigInteger,  "\"type\": \"string\", \"pattern\": \"^-?[0-9]+$\"" ); // https://www.regextester.com/
            AddType (map, standard.DateTime,    "\"type\": \"string\", \"format\": \"date-time\"" );
            AddType (map, standard.Guid,        "\"type\": \"string\", \"pattern\": \"^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$\"" );
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            var typeName = type.Name;
            sb.AppendLine($"        \"{typeName}\": {{");
            sb.AppendLine($"            {definition}");
            sb.Append    ( "        }");
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
                sb.AppendLine($"        \"{type.Name}\": {{");
                sb.AppendLine($"            \"enum\": [");
                bool firstValue = true;
                foreach (var enumValue in enumValues) {
                    Delimiter(sb, Next, ref firstValue);
                    sb.Append($"                \"{enumValue}\"");
                }
                sb.AppendLine();
                sb.AppendLine("            ]");
                sb.Append    ("        }");
                return new EmitType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var context         = new TypeContext (generator, null, type);
            var fields          = type.Fields;
            int maxFieldName    = fields.MaxLength(field => field.name.Length);
            sb.AppendLine($"        \"{type.Name}\": {{");
            var baseType    = type.BaseType;
            var unionType   = type.UnionType;
            if (unionType == null) {
                sb.AppendLine($"            \"type\": \"object\",");
                if (baseType != null)
                    sb.AppendLine($"            \"extends\": {{ {Ref(baseType, true, context)} }},");
                if (type.IsStruct)
                    sb.AppendLine($"            \"isStruct\": true,");
                if (type.IsAbstract)
                    sb.AppendLine($"            \"isAbstract\": true,");
                var docs = GetDescription("            ", type.docs, ",");
                if (docs != "")
                    sb.AppendLine(docs);
            } else {
                sb.AppendLine($"            \"discriminator\": \"{unionType.discriminator}\",");
                sb.AppendLine($"            \"oneOf\": [");
                bool firstElem = true;
                foreach (var polyType in unionType.types) {
                    Delimiter(sb, Next, ref firstElem);
                    sb.Append($"                {{ {Ref(polyType.typeDef, true, context)} }}");
                }
                sb.AppendLine();
                sb.AppendLine($"            ],");
            }
            var key = type.KeyField; 
            if (key != null && key != "id") {
                sb.AppendLine($"            \"key\": \"{key}\",");    
            }
            sb.AppendLine($"            \"properties\": {{");
            bool    firstField      = true;
            var     requiredFields  = new List<string>(fields.Count);
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                sb.Append($"                \"{discriminator}\":{indent} {{ \"enum\": [\"{discriminant}\"] }}");
                firstField = false;
                requiredFields.Add(discriminator);
            }
            if (unionType != null ) {
                var disc = unionType.discriminator;
                maxFieldName    = Math.Max(maxFieldName, disc.Length);
                var indent      = Indent(maxFieldName, disc);
                var discriminators = string.Join(", ", unionType.types.Select(polyType => $"\"{polyType.discriminant}\""));
                sb.Append($"                \"{disc}\":{indent} {{ \"enum\": [{discriminators}] }}");
                firstField = false;
                requiredFields.Add(disc);
            }
            foreach (var field in fields) {
                // if (generator.IsDerivedField(type, field))  JSON Schema list all properties
                //    continue;
                bool required = field.required;
                var fieldType = GetFieldType(field, context, required);
                var indent = Indent(maxFieldName, field.name);
                if (required)
                    requiredFields.Add(field.name);
                var autoStr = field.isAutoIncrement ? ", \"isAutoIncrement\": true" : "";
                var relStr  = GetRelation(field, context);
                Delimiter(sb, Next, ref firstField);
                var docs    = GetDescription(", ", field.docs, "");
                sb.Append($"                \"{field.name}\":{indent} {{ {fieldType}{autoStr}{relStr}{docs} }}");
            }
            sb.AppendLine();
            sb.AppendLine("            },");
            if (requiredFields.Count > 0 ) {
                bool firstReq = true;
                sb.AppendLine("            \"required\": [");
                foreach (var item in requiredFields) {
                    Delimiter(sb, Next, ref firstReq);
                    sb.Append ($"                \"{item}\"");
                }
                sb.AppendLine();
                sb.AppendLine("            ],");
            }
            var additionalProperties = unionType != null ? "true" : "false";
            sb.Append($"            \"additionalProperties\": {additionalProperties}");
            if (type.IsSchema) {
                EmitServiceType(type, context, sb);
            } else {
                sb.AppendLine();
            }
            sb.Append     ("        }");
            return new EmitType(type, sb);
        }
        
        private static void EmitServiceType(TypeDef type, TypeContext context, StringBuilder sb) {
            var     commands    = type.Commands;
            bool    firstField  = true;
            int maxFieldName    = commands.MaxLength(field => field.name.Length);
            sb.AppendLine(",");
            sb.AppendLine("            \"commands\": {");
            foreach (var command in commands) {
                var commandParam    = GetFieldType(command.param,  context, command.param.required);
                var commandResult   = GetFieldType(command.result, context, command.result.required);
                var description     = GetDescription(",\n                    ", command.docs, "");
                var indent          = Indent(maxFieldName, command.name);
                Delimiter(sb, Next, ref firstField);
                var signature = $"\"param\": {{ {commandParam} }}, \"result\": {{ {commandResult} }}";
                sb.Append($"                \"{command.name}\":{indent} {{ {signature}{description} }}");
            }
            sb.AppendLine("\n            }");
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context, bool required) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"\"type\": {Opt(required, "array")}, \"items\": {elementTypeName}";
            }
            if (field.isDictionary) {
                var valueTypeName = GetElementType(field, context);
                return $"\"additionalProperties\": {valueTypeName}, \"type\": \"object\"";
            }
            return GetTypeName(field.type, context, required);
        }
        
        private static string GetElementType(FieldDef field, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, context, true);
            if (field.isNullableElement)
                return $"{{ \"oneOf\": [{{ {elementTypeName} }}, {{ \"type\": \"null\"}}]}}";
            return $"{{ {elementTypeName} }}";
        }
        
        private static string GetTypeName(TypeDef type, TypeContext context, bool required) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return ""; // allow any type                
            if (type == standard.String || type == standard.JsonKey)
                return $"\"type\": {Opt(required, "string")}";
            if (type == standard.Boolean)
                return $"\"type\": {Opt(required, "boolean")}";
            return Ref(type, required, context);
        }
        
        private static string GetRelation(FieldDef field, TypeContext context) {
            if (field.relation == null)
                return "";
            return $", \"relation\": \"{field.relation }\"";
        }
        
        private static string GetDescription(string prefix, string docs, string suffix) {
            if (docs == null)
                return "";
            docs = docs.Replace("\n", "\\n");
            docs = docs.Replace("\"", "'");
            return $"{prefix}\"description\": \"{docs}\"{suffix}";
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                var emitFile = pair.Value;
                sb.Clear();
                sb.AppendLine("{");
                sb.AppendLine( "    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLine($"    \"$comment\": \"{Note}\",");
                var first = emitFile.emitTypes.FirstOrDefault();
                if (first != null && generator.separateTypes.Contains(first.type)) {
                    var entityName = first.type.Name;
                    sb.AppendLine($"    \"$ref\": \"#/definitions/{entityName}\",");
                }
                sb.Append    ("    \"definitions\": {");
                emitFile.header = sb.ToString();
            }
        }
        
        private void EmitFileFooters(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                var emitFile = pair.Value;
                sb.Clear();
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");
                emitFile.footer = sb.ToString();
            }
        }
        
        private static string Ref(TypeDef type, bool required, TypeContext context) {
            var generator   = context.generator;
            var name        = type.Name;
            // if (generator.IsUnionType(type))
            //    name = $"{type.Name}_Union";
            var typePath    = type.Path;
            var contextPath = context.type.Path;
            bool samePath   = typePath == contextPath;
            var prefix      = samePath ? "" : $"./{typePath}{generator.fileExt}";
            var refType = $"\"$ref\": \"{prefix}#/definitions/{name}\"";
            if (!required)
                return $"\"oneOf\": [{{ {refType} }}, {{\"type\": \"null\"}}]";
            return refType;
        }
        
        private static string Opt (bool required, string name) {
            if (required)
                return $"\"{name}\"";
            return $"[\"{name}\", \"null\"]";
        }
    }
}