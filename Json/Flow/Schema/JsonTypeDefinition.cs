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
    /// [RFC 8927 - JSON Type Definition] https://datatracker.ietf.org/doc/rfc8927/
    public class JsonTypeDefinition
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Next = ",\r\n";
        
        public JsonTypeDefinition (Generator generator, string name) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.schema.StandardTypes);
            GenerateSchema(name);
        }
        
        private void GenerateSchema(string name) {
            var sb = new StringBuilder();
            // emit custom types
            foreach (var type in generator.types) {
                sb.Clear();
                var result = EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupToSinglePackage(name);
            EmitPackageHeaders(sb);
            EmitPackageFooters(sb);
            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}", Next); // $"{ns.Replace(".", "/")}.ts");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Unit8,         "uint8" );
            AddType (map, standard.Int16,         "int16" );
            AddType (map, standard.Int32,         "int32" );
            AddType (map, standard.Int64,         "int64" );
                
            AddType (map, standard.Double,        "float64" );
            AddType (map, standard.Float,         "float32" );
                
            AddType (map, standard.BigInteger,    "string" ); // https://www.regextester.com/   ^-?[0-9]+$
            AddType (map, standard.DateTime,      "timestamp" );
            return map;
        }
        
        private EmitType EmitStandardType(TypeDef type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append($"        \"type\": \"{definition}\"");
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
                var fields          = type.Fields;
                int maxFieldName    = fields.MaxLength(field => field.name.Length);
                
                string  discriminator   = null;
                var     discriminant    = type.Discriminant;
                if (discriminant != null) {
                    var baseType    = type.BaseType;
                    discriminator   = baseType.UnionType.discriminator;
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var unionType = type.UnionType;
                sb.AppendLine($"        \"{type.Name}\": {{");
                if (unionType == null) {
                } else {
                    sb.AppendLine($"            \"discriminator\": \"{unionType.discriminator}\",");
                    sb.AppendLine( "            \"mapping\": {");
                    bool firstElem = true;
                    int maxDiscriminant = unionType.types.MaxLength(t => t.Discriminant.Length);
                    foreach (var polyType in unionType.types) {
                        Delimiter(sb, Next, ref firstElem);
                        var discName = polyType.Discriminant;
                        var indent = Indent(maxDiscriminant, discName);
                        sb.Append($"                \"{discName}\": {indent}{{ {Ref(polyType, true, context)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("            },");
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
                // fields
                foreach (var field in fields) {
                    // if (generator.IsDerivedField(type, field))  JSON Schema list all properties
                    //    continue;
                    bool required = field.required;
                    var fieldType = GetFieldType(field.type, context, required);
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
                return new EmitType(type, generator, sb, imports);
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
                return new EmitType(type, generator, sb);
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(TypeDef type, TypeContext context, bool required) {
            var standard = context.generator.schema.StandardTypes;
            if (type == standard.JsonValue) {
                return ""; // allow any type
            }
            if (type == standard.String) {
                return "\"type\": \"string\"";
            }
            if (type == standard.Boolean) {
                return "\"type\": \"boolean\"";
            }
            if (type.IsArray) {
                var elementMapper = type.ElementType;
                var elementTypeName = GetFieldType(elementMapper, context, true);
                return $"\"elements\": {{ {elementTypeName} }}";
            }
            if (type.IsDictionary) {
                var valueMapper = type.ElementType;
                var valueTypeName = GetFieldType(valueMapper, context, true);
                return $"\"type\": \"object\", \"additionalProperties\": {{ {valueTypeName} }}";
            }
            context.imports.Add(type);
            return Ref(type, required, context);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine("{");
                // sb.AppendLine( "    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLine($"    \"$comment\": \"{Note}\",");
                sb.Append    ("    \"definitions\": {");
                package.header = sb.ToString();
            }
        }
        
        private void EmitPackageFooters(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");
                package.footer = sb.ToString();
            }
        }
        
        private static string Ref(TypeDef type, bool required, TypeContext context) {
            var name = context.generator.GetTypeName(type);
            // var typePackage     = generator.GetPackageName(type);
            // var ownerPackage    = generator.GetPackageName(context.owner);
            // bool samePackage    = typePackage == ownerPackage;
            // var prefix          = samePackage ? "" : $"./{typePackage}{generator.fileExt}";
            var nullableStr = required ? "" : ", \"nullable\": true";
            var refType = $"\"ref\": \"{name}\"{nullableStr}";
            return refType;
        }
    }
}