// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Utils;
using static Friflo.Json.Flow.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Flow.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Flow.Schema
{
    public partial class JsonSchemaGenerator
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
            var sb = new StringBuilder();
            // emit custom types
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
            AddType (map, standard.Uint8,         "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 255" );
            AddType (map, standard.Int16,         "\"type\": \"integer\", \"minimum\": -32768, \"maximum\": 32767" );
            AddType (map, standard.Int32,         "\"type\": \"integer\", \"minimum\": -2147483648, \"maximum\": 2147483647" );
            AddType (map, standard.Int64,         "\"type\": \"integer\", \"minimum\": -9223372036854775808, \"maximum\": 9223372036854775807" );
                
            AddType (map, standard.Double,        "\"type\": \"number\"" );
            AddType (map, standard.Float,         "\"type\": \"number\"" );
                
            AddType (map, standard.BigInteger,    "\"type\": \"string\", \"pattern\": \"^-?[0-9]+$\"" ); // https://www.regextester.com/
            AddType (map, standard.DateTime,      "\"type\": \"string\", \"format\": \"date-time\"" );
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
            var context         = new TypeContext (generator, null, type);
            var standardType    = EmitStandardType(type, sb);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsComplex) {
                var fields          = type.Fields;
                int maxFieldName    = fields.MaxLength(field => field.name.Length);
                
                string  discriminator   = null;
                var     discriminant    = type.Discriminant;
                sb.AppendLine($"        \"{type.Name}\": {{");
                var baseType    = type.BaseType;
                if (discriminant != null) {
                    discriminator   = baseType.UnionType.discriminator;
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var unionType = type.UnionType;
                if (unionType == null) {
                    sb.AppendLine($"            \"type\": \"object\",");
                    if (baseType != null)
                        sb.AppendLine($"            \"extends\": {{ {Ref(baseType, true, context)} }},");
                } else {
                    sb.AppendLine($"            \"discriminator\": \"{unionType.discriminator}\",");
                    sb.AppendLine($"            \"oneOf\": [");
                    bool firstElem = true;
                    foreach (var polyType in unionType.types) {
                        Delimiter(sb, Next, ref firstElem);
                        sb.Append($"                {{ {Ref(polyType, true, context)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"            ],");
                }
                sb.AppendLine($"            \"properties\": {{");
                bool firstField     = true;
                var requiredFields  = new List<string>();
                if (discriminant != null) {
                    var indent = Indent(maxFieldName, discriminator);
                    sb.Append($"                \"{discriminator}\":{indent} {{ \"enum\": [\"{discriminant}\"] }}");
                    firstField = false;
                    requiredFields.Add(discriminator);
                }
                foreach (var field in fields) {
                    // if (generator.IsDerivedField(type, field))  JSON Schema list all properties
                    //    continue;
                    bool required = field.required;
                    var fieldType = GetFieldType(field, context, required);
                    var indent = Indent(maxFieldName, field.name);
                    if (required)
                        requiredFields.Add(field.name);
                    Delimiter(sb, Next, ref firstField);
                    sb.Append($"                \"{field.name}\":{indent} {{ {fieldType} }}");
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
                sb.AppendLine($"            \"additionalProperties\": {additionalProperties}");
                sb.Append     ("        }");
                return new EmitType(type, sb);
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
        
        // Note: static by intention
        private static string GetFieldType(FieldDef field, TypeContext context, bool required) {
            var type = field.type;
            if (field.isArray) {
                var elementTypeName = GetTypeName(type, context, true);
                return $"\"type\": {Opt(required, "array")}, \"items\": {{ {elementTypeName} }}";
            }
            if (field.isDictionary) {
                var valueTypeName = GetTypeName(type, context, true);
                return $"\"type\": \"object\", \"additionalProperties\": {{ {valueTypeName} }}";
            }
            return GetTypeName(type, context, required);
        }
        
        private static string GetTypeName(TypeDef type, TypeContext context, bool required) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return ""; // allow any type
            if (type == standard.String)
                return $"\"type\": {Opt(required, "string")}";
            if (type == standard.Boolean)
                return $"\"type\": {Opt(required, "boolean")}";
            return Ref(type, required, context);
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
                return $"\"oneOf\": [{{\"type\": \"null\"}}, {{ {refType} }}]";
            return refType;
        }
        
        private static string Opt (bool required, string name) {
            if (required)
                return $"\"{name}\"";
            return $"[\"{name}\", \"null\"]";
        }
    }
}