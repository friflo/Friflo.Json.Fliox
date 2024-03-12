// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;
// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class JsonSchemaGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Next = ",\n";
        
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
            OpenAPI.Generate(generator);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            
            AddType (map, standard.Uint8,       "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 255" );
            AddType (map, standard.Int16,       "\"type\": \"integer\", \"minimum\": -32768, \"maximum\": 32767" );
            AddType (map, standard.Int32,       "\"type\": \"integer\", \"minimum\": -2147483648, \"maximum\": 2147483647" );
            AddType (map, standard.Int64,       "\"type\": \"integer\", \"minimum\": -9223372036854775808, \"maximum\": 9223372036854775807" );
            
            // NON_CLS
            AddType (map, standard.Int8,        "\"type\": \"integer\", \"minimum\": -128, \"maximum\": 127" );
            AddType (map, standard.UInt16,      "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 65535" );
            AddType (map, standard.UInt32,      "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 4294967295" );
            AddType (map, standard.UInt64,      "\"type\": \"integer\", \"minimum\": 0, \"maximum\": 18446744073709551615" );
                
            AddType (map, standard.Double,      "\"type\": \"number\"" );
            AddType (map, standard.Float,       "\"type\": \"number\"" );
                
            AddType (map, standard.BigInteger,  "\"type\": \"string\", \"pattern\": \"^-?[0-9]+$\"" ); // https://www.regextester.com/
            AddType (map, standard.DateTime,    "\"type\": \"string\", \"format\": \"date-time\", \"default\": \"2023-01-01T00:00:00Z\"" );
            AddType (map, standard.Guid,        "\"type\": \"string\", \"pattern\": \"^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$\"" );
            AddType (map, standard.JsonKey,     "\"oneOf\": [{ \"type\": \"string\" }, { \"type\": \"integer\" }]");
            AddType (map, standard.JsonTable,   "\"type\": \"array\", \"items\": { \"type\": \"array\" }");
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            var typeName = type.Name;
            sb.AppendLF($"        \"{typeName}\": {{");
            sb.AppendLF($"            {definition}");
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
                sb.AppendLF($"        \"{type.Name}\": {{");
                sb.AppendLF($"            \"enum\": [");
                bool firstValue = true;
                var maxNameLen     = 0;
                foreach (var enumValue in enumValues) {
                    maxNameLen = Math.Max(maxNameLen, enumValue.name.Length);
                    Delimiter(sb, Next, ref firstValue);
                    sb.Append($"                \"{enumValue.name}\"");
                }
                sb.AppendLF();
                sb.Append("            ]");
                var docCount = enumValues.Count(e => e.doc != null);
                if (docCount > 0 ) {
                    sb.AppendLF($",\n            \"descriptions\": {{");
                    firstValue = true;
                    foreach (var enumValue in enumValues) {
                        var doc = GetDoc("", enumValue.doc, "");
                        if (doc == "")
                            continue;
                        Delimiter(sb, Next, ref firstValue);
                        var indent      = Indent(maxNameLen, enumValue.name);
                        sb.Append($"                \"{enumValue.name}\": {indent}{doc}");
                    }
                    sb.AppendLF();
                    sb.AppendLF("            }");
                }
                sb.AppendLF();
                sb.Append    ("        }");
                return new EmitType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var context         = new TypeContext (generator, null, type);
            var fields          = type.Fields;
            int maxFieldName    = fields.MaxLength(field => field.name.Length);
            sb.AppendLF($"        \"{type.Name}\": {{");
            var baseType    = type.BaseType;
            var unionType   = type.UnionType;
            if (unionType == null) {
                sb.AppendLF($"            \"type\": \"object\",");
                if (baseType != null)
                    sb.AppendLF($"            \"extends\": {{ {Ref(baseType, true, context)} }},");
                if (type.IsStruct)
                    sb.AppendLF($"            \"isStruct\": true,");
                if (type.IsAbstract)
                    sb.AppendLF($"            \"isAbstract\": true,");
                var doc = GetDoc("            \"description\": ", type.doc, ",");
                if (doc != "")
                    sb.AppendLF(doc);
            } else {
                sb.AppendLF($"            \"discriminator\": \"{unionType.discriminator}\",");
                sb.AppendLF($"            \"oneOf\": [");
                bool firstElem = true;
                foreach (var polyType in unionType.types) {
                    Delimiter(sb, Next, ref firstElem);
                    sb.Append($"                {{ {Ref(polyType.typeDef, true, context)} }}");
                }
                sb.AppendLF();
                sb.AppendLF($"            ],");
            }
            var key = type.KeyField?.name; 
            if (key != null && key != "id") {
                sb.AppendLF($"            \"key\": \"{key}\",");    
            }
            sb.AppendLF($"            \"properties\": {{");
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
                var disc        = unionType.discriminator;
                var doc         = GetDoc(", \"description\": ", unionType.doc, "");
                maxFieldName    = Math.Max(maxFieldName, disc.Length);
                var indent      = Indent(maxFieldName, disc);
                var discriminators = string.Join(", ", unionType.types.Select(polyType => $"\"{polyType.discriminant}\""));
                sb.Append($"                \"{disc}\":{indent} {{ \"enum\": [{discriminators}]{doc} }}");
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
                var autoStr = field.isAutoIncrement ? "\"isAutoIncrement\": true" : "";
                var relStr  = GetRelation(field, context);
                Delimiter(sb, Next, ref firstField);
                var doc     = GetDoc("\"description\": ", field.doc, "");
                var values  = JoinValues(new [] { fieldType, autoStr, relStr, doc });
                sb.Append($"                \"{field.name}\":{indent} {{ {values} }}");
            }
            sb.AppendLF();
            sb.AppendLF("            },");
            if (requiredFields.Count > 0 ) {
                bool firstReq = true;
                sb.AppendLF("            \"required\": [");
                foreach (var item in requiredFields) {
                    Delimiter(sb, Next, ref firstReq);
                    sb.Append ($"                \"{item}\"");
                }
                sb.AppendLF();
                sb.AppendLF("            ],");
            }
            var additionalProperties = unionType != null ? "true" : "false";
            sb.Append($"            \"additionalProperties\": {additionalProperties}");
            EmitMessages("commands", type.Commands, context, sb);
            EmitMessages("messages", type.Messages, context, sb);
            sb.AppendLF();

            sb.Append     ("        }");
            return new EmitType(type, sb);
        }
        
        private static void EmitMessages(string type, IReadOnlyList<MessageDef> messageDefs, TypeContext context, StringBuilder sb) {
            if (messageDefs == null)
                return;
            bool    firstField  = true;
            int maxFieldName    = messageDefs.MaxLength(field => field.name.Length);
            sb.AppendLF(",");
            sb.AppendLF($"            \"{type}\": {{");
            foreach (var messageDef in messageDefs) {
                var param           = GetMessageArg("param",  messageDef.param,  context);
                var result          = GetMessageArg("result", messageDef.result, context);
                var doc             = GetDoc(",\n                    \"description\": ", messageDef.doc, "");
                var indent          = Indent(maxFieldName, messageDef.name);
                Delimiter(sb, Next, ref firstField);
                var argDelimiter    = param.Length > 0 && result.Length > 0 ? ", " : "";
                var signature       = $"{param}{argDelimiter}{result}";
                sb.Append($"                \"{messageDef.name}\":{indent} {{ {signature}{doc} }}");
            }
            sb.Append("\n            }");
        }
        
        private static string GetMessageArg(string name, FieldDef fieldDef, TypeContext context) {
            if (fieldDef == null)
                return "";
            var argType = GetFieldType(fieldDef, context, fieldDef.required);
            return $"\"{name}\": {{ {argType} }}";
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
            if (type == standard.JsonEntity)
                return ""; // allow any type                
            if (type == standard.String || type == standard.ShortString)
                return $"\"type\": {Opt(required, "string")}";
            if (type == standard.Boolean)
                return $"\"type\": {Opt(required, "boolean")}";
            return Ref(type, required, context);
        }
        
        private static string GetRelation(FieldDef field, TypeContext context) {
            if (field.relation == null)
                return "";
            return $"\"relation\": \"{field.relation }\"";
        }
        
        internal static string GetDoc(string prefix, string docs, string suffix) {
            if (docs == null)
                return "";
            docs = docs.Replace("\n", "\\n");
            docs = docs.Replace("\"", "'");
            return $"{prefix}\"{docs}\"{suffix}";
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                var emitFile = pair.Value;
                sb.Clear();
                sb.AppendLF("{");
                sb.AppendLF( "    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLF($"    \"$comment\": \"{Note}\",");
                var first = emitFile.emitTypes.FirstOrDefault();
                if (first != null && generator.separateTypes.Contains(first.type)) {
                    var entityName = first.type.Name;
                    sb.AppendLF($"    \"$ref\": \"#/definitions/{entityName}\",");
                }
                sb.Append    ("    \"definitions\": {");
                emitFile.header = sb.ToString();
            }
        }
        
        private void EmitFileFooters(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                var emitFile = pair.Value;
                sb.Clear();
                sb.AppendLF();
                sb.AppendLF("    }");
                sb.AppendLF("}");
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
        
        private static string JoinValues (IEnumerable<string> values ) {
            var sb = new StringBuilder();
            bool first = true;
            foreach (var value in values) {
                if (string.IsNullOrEmpty(value))
                    continue;
                if (first) {
                    first = false;
                } else {
                    sb.Append(", ");
                }
                sb.Append(value);
            }
            return sb.ToString();
        }

    }
}