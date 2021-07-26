// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Native;
using Friflo.Json.Flow.Schema.Utils;
using static Friflo.Json.Flow.Schema.Generator;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.lab
{
    /// [RFC 8927 - JSON Type Definition] https://datatracker.ietf.org/doc/rfc8927/
    public class JsonTypeDefinition
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  readonly   Dictionary<TypeDef, string> primitiveTypes;
        private  const      string                      Next = ",\r\n";
        
        public JsonTypeDefinition (Generator generator, string name) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes (generator.schema.StandardTypes);
            primitiveTypes  = GetPrimitiveTypes(generator.schema.StandardTypes);
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
            AddType (map, standard.BigInteger,    "\"string\"" ); // https://www.regextester.com/   ^-?[0-9]+$
            return map;
        }
        
        private static Dictionary<TypeDef, string> GetPrimitiveTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,       "boolean" );
            AddType (map, standard.String,        "string" );
            
            AddType (map, standard.Unit8,         "uint8" );
            AddType (map, standard.Int16,         "int16" );
            AddType (map, standard.Int32,         "int32" );
            AddType (map, standard.Int64,         "int64" );
               
            AddType (map, standard.Double,        "double" );
            AddType (map, standard.Float,         "float" );
               
            AddType (map, standard.DateTime,      "timestamp" );
            return map;
        }
        
        private EmitType EmitStandardType(TypeDef type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append($"        \"{type.Name}\": {definition}");
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
                
                var     discriminant    = type.Discriminant;
                if (discriminant != null) {
                    var baseType    = type.BaseType;
                    var discriminator   = baseType.UnionType.discriminator;
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
                        sb.Append($"                \"{discName}\": {indent}{{ {Ref(polyType)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("            },");
                }
                sb.AppendLine($"            \"properties\": {{");
                bool firstField     = true;

                // fields
                foreach (var field in fields) {
                    bool required = field.required;
                    var fieldType = GetFieldType(field.type, context);
                    var indent = Indent(maxFieldName, field.name);
                    Delimiter(sb, Next, ref firstField);
                    var nullableStr = required ? "" : ", \"nullable\": true";
                    sb.Append($"                \"{field.name}\":{indent} {{ {fieldType}{nullableStr} }}");
                }
                sb.AppendLine();
                sb.AppendLine("            }");

                // var additionalProperties = unionType != null ? "true" : "false"; 
                // sb.AppendLine($"            \"additionalProperties\": {additionalProperties}");
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
        private string GetFieldType(TypeDef type, TypeContext context) {
            var standard = context.generator.schema.StandardTypes;
            if (type == standard.JsonValue) {
                return ""; // allow any type
            }
            if (primitiveTypes.TryGetValue(type, out var definition)) {
                return $"\"type\": \"{definition}\"";
            }
            if (type.IsArray) {
                var elementMapper = type.ElementType;
                var elementTypeName = GetFieldType(elementMapper, context);
                return $"\"elements\": {{ {elementTypeName} }}";
            }
            if (type.IsDictionary) {
                var valueMapper = type.ElementType;
                var valueTypeName = GetFieldType(valueMapper, context);
                return $"\"values\": {{ {valueTypeName} }}";
            }
            context.imports.Add(type);
            return $"{Ref(type)}";
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
        
        private static string Ref(TypeDef type) {
            var name = type.Name;
            return $"\"ref\":  \"{name}\"";
        }
        
        public static Generator Generate (TypeStore typeStore, ICollection<Type> rootTypes, string name, ICollection<string> stripNamespaces = null, ICollection<Type> separateTypes = null) {
            typeStore.AddMappers(rootTypes);
            var schema      = new NativeTypeSchema(typeStore, separateTypes);
            var generator   = new Generator(schema, stripNamespaces, ".json");
            var _           = new JsonTypeDefinition(generator, name);
            return generator;
        }
    }
}