// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Misc
{
    /// [RFC 8927 - JSON Type Definition] https://datatracker.ietf.org/doc/rfc8927/
    /// Using JsonTypeDefinition was discarded as polymorphic cannot represented by tagged unions without violation
    /// the specification. Because "Properties forms inside mapping cannot be nullable ..." but this is required. See:
    /// [JSON Type Definition | Ajv JSON schema validator] https://ajv.js.org/json-type-definition.html#discriminator-form
    public class JsonTypeDefinition
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  readonly   Dictionary<TypeDef, string> primitiveTypes;
        private  const      string                      Next = ",\n";
        
        private JsonTypeDefinition (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes (generator.standardTypes);
            primitiveTypes  = GetPrimitiveTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator, string name) {
            var emitter = new JsonTypeDefinition(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupToSingleFile(name);
            emitter.EmitFileHeaders(sb);
            emitter.EmitFileFooters(sb);
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}", Next);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.BigInteger,    "\"string\"" ); // https://www.regextester.com/   ^-?[0-9]+$
            return map;
        }
        
        private static Dictionary<TypeDef, string> GetPrimitiveTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,       "boolean" );
            AddType (map, standard.String,        "string" );
            
            AddType (map, standard.Uint8,         "uint8" );
            AddType (map, standard.Int16,         "int16" );
            AddType (map, standard.Int32,         "int32" );
            AddType (map, standard.Int64,         "int64" );
            
            // NON_CLS
            AddType (map, standard.Int8,          "int8" );
            AddType (map, standard.UInt16,        "uint16" );
            AddType (map, standard.UInt32,        "uint32" );
            AddType (map, standard.UInt64,        "uint64" );
               
            AddType (map, standard.Double,        "double" );
            AddType (map, standard.Float,         "float" );
               
            AddType (map, standard.DateTime,      "timestamp" );
            AddType (map, standard.JsonKey,       "string" );
            AddType (map, standard.ShortString,   "string" );
            return map;
        }
        
        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append($"        \"{type.Name}\": {definition}");
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
                foreach (var enumValue in enumValues) {
                    Delimiter(sb, Next, ref firstValue);
                    sb.Append($"                \"{enumValue.name}\"");
                }
                sb.AppendLF();
                sb.AppendLF("            ]");
                sb.Append    ("        }");
                return new EmitType(type, sb);
            }
            return null;
        }
        
        private EmitType EmitClassType(TypeDef type, StringBuilder sb) {
            var context         = new TypeContext (generator, null, type);
            var fields          = type.Fields;
            int maxFieldName    = fields.MaxLength(field => field.name.Length);
            var unionType       = type.UnionType;
            sb.AppendLF($"        \"{type.Name}\": {{");
            if (unionType == null) {
            } else {
                sb.AppendLF($"            \"discriminator\": \"{unionType.discriminator}\",");
                sb.AppendLF( "            \"mapping\": {");
                bool firstElem = true;
                int maxDiscriminant = unionType.types.MaxLength(t => t.discriminant.Length);
                foreach (var polyType in unionType.types) {
                    Delimiter(sb, Next, ref firstElem);
                    var discName = polyType.discriminant;
                    var indent = Indent(maxDiscriminant, discName);
                    sb.Append($"                \"{discName}\": {indent}{{ {Ref(polyType.typeDef)} }}");
                }
                sb.AppendLF();
                sb.AppendLF("            },");
            }
            sb.AppendLF($"            \"properties\": {{");
            bool firstField     = true;
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (discriminant != null) {
                maxFieldName = Math.Max(maxFieldName, discriminator.Length);
            }
            foreach (var field in fields) {
                bool required = field.required;
                var fieldType = GetFieldType(field, context);
                var indent = Indent(maxFieldName, field.name);
                Delimiter(sb, Next, ref firstField);
                var nullableStr = required ? "" : fieldType.Length == 0 ? "\"nullable\": true" : ", \"nullable\": true";
                sb.Append($"                \"{field.name}\":{indent} {{ {fieldType}{nullableStr} }}");
            }
            sb.AppendLF();
            sb.AppendLF("            }");
            // var additionalProperties = unionType != null ? "true" : "false"; 
            // sb.AppendLF($"            \"additionalProperties\": {additionalProperties}");
            sb.Append     ("        }");
            return new EmitType(type, sb);
        }
        
        private string GetFieldType(FieldDef field, TypeContext context) {
            var type = field.type;
            if (field.isArray) {
                var elementTypeName = GetTypeName(type, context);
                return $"\"elements\": {{ {elementTypeName} }}";
            }
            if (field.isDictionary) {
                var valueTypeName = GetTypeName(type, context);
                return $"\"values\": {{ {valueTypeName} }}";
            }
            return GetTypeName(type, context);
        }
        
        private string GetTypeName(TypeDef type, TypeContext context) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return ""; // allow any type
            if (type == standard.JsonEntity)
                return ""; // allow any type
            if (primitiveTypes.TryGetValue(type, out var definition))
                return $"\"type\": \"{definition}\"";
            return $"{Ref(type)}";
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                var emitFile = pair.Value;
                sb.Clear();
                sb.AppendLF("{");
                // sb.AppendLF( "    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLF($"    \"$comment\": \"{Note}\",");
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
        
        private static string Ref(TypeDef type) {
            var name = type.Name;
            return $"\"ref\":  \"{name}\"";
        }
        
        public static Generator Generate(NativeTypeOptions options, string name) {
            var schema      = NativeTypeSchema.Create(options.rootType);
            var sepTypes    = schema.TypesAsTypeDefs(options.separateTypes);
            var generator   = new Generator(schema, options.fileExt ?? ".jtd.json", options.replacements, sepTypes, options.getPath);
            Generate(generator, name);
            return generator;
        }
        
        public static Generator Generate(GeneratorOptions options) {
            var generator   = new Generator(options.typeSchema, ".jtd.json", options.replacements, options.separateTypes);
            var name        = options.typeSchema.RootType.Name;
            Generate(generator, name);
            return generator;
        }
        
        public static SchemaModel GenerateJTD(GeneratorOptions options) {
            var generator = Generate(options);
            return new SchemaModel(options.type, options.name, "application/json", ".jtd", generator.files);
        }
    }
}