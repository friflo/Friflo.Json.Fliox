// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.Utils;
using static Friflo.Json.Flow.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Flow.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Flow.Schema
{
    public partial class CSharpGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  readonly   Dictionary<TypeDef, string> customTypes;

        private CSharpGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
            customTypes     = GetCustomTypes  (generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new CSharpGenerator(generator);
            var sb = new StringBuilder();
            // emit custom types
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPath(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.
            emitter.EmitFileHeaders(sb);
            emitter.EmitFileFooters(sb);
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,       "bool" );
            AddType (map, standard.String,        "string" );

            AddType (map, standard.Uint8,         "byte" );
            AddType (map, standard.Int16,         "short" );
            AddType (map, standard.Int32,         "int" );
            AddType (map, standard.Int64,         "long" );
               
            AddType (map, standard.Double,        "double" );
            AddType (map, standard.Float,         "float" );
            return map;
        }
        
        private static Dictionary<TypeDef, string> GetCustomTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.BigInteger,      "System.Numerics" );
            AddType (map, standard.DateTime,        "System" );
            AddType (map, standard.JsonValue,       "Friflo.Json.Flow.Mapper" );
            return map;
        }

        /* private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append("export type ");
            sb.Append(definition);
            sb.AppendLine(";");
            sb.AppendLine();
            return new EmitType(type, sb);
        } */
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            /* var standardType    = EmitStandardType(type, sb);
            if (standardType != null ) {
                return standardType;
            } */
            if (type.IsComplex) {
                var dependencies = new List<TypeDef>();
                var fields          = type.Fields;

                string  discriminator   = null;
                var     discriminant    = type.Discriminant;
                var     extendsStr      = "";
                var baseType    = type.BaseType;
                if (discriminant != null) {
                    discriminator   = baseType.UnionType.discriminator;
                    extendsStr = $": {baseType.Name} ";
                    dependencies.Add(baseType);
                } else {
                    if (baseType != null) {
                        extendsStr = $": {baseType.Name} ";
                        imports.Add(baseType);
                        dependencies.Add(baseType);
                    }
                }
                var unionType = type.UnionType;
                if (unionType == null) {
                    sb.AppendLine($"public class {type.Name} {extendsStr}{{");
                } else {
                    /* sb.AppendLine($"export type {type.Name}_Union =");
                    foreach (var polyType in unionType.types) {
                        sb.AppendLine($"    | {polyType.Name}");
                        imports.Add(polyType);
                    }
                    sb.AppendLine($";");
                    sb.AppendLine(); */
                    sb.AppendLine($"public  abstract class {type.Name} {extendsStr}{{");
                    /* sb.AppendLine($"    abstract {unionType.discriminator}:");
                    foreach (var polyType in unionType.types) {
                        sb.AppendLine($"        | \"{polyType.Discriminant}\"");
                    }
                    sb.AppendLine($"    ;"); */
                }
                if (discriminant != null) {
                    // var indent = Indent(maxFieldName, discriminator);
                    // sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
                }
                var emitFields = new List<EmitField>();
                foreach (var field in fields) {
                    if (field.IsDerivedField)
                        continue;
                    bool notNull = field.required || field.isArray || field.type == context.standardTypes.String || !standardTypes.ContainsKey(field.type);
                    var fieldType = GetFieldType(field, context);
                    var emitField = new EmitField(fieldType, field.name, notNull);
                    emitFields.Add(emitField);
                }
                int maxFieldName    = emitFields.MaxLength(field => field.type.Length);
                foreach (var field in emitFields) {
                    var indent  = Indent(maxFieldName, field.type);
                    var nullStr = field.notNull ? " " : "?";
                    sb.AppendLine($"    {field.type}{nullStr}{indent} {field.name};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, sb, imports, dependencies);
            }
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                sb.AppendLine($"public enum {type.Name} {{");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    {enumValue},");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, sb);
            }
            return null;
        }
        
        // Note: static by intention
        private string GetFieldType(FieldDef field, TypeContext context) {
            var type = field.type;
            if (field.isArray) {
                var elementTypeName = GetTypeName(type, context);
                return $"List<{elementTypeName}>";
            }
            if (field.isDictionary) {
                var valueTypeName = GetTypeName(type, context);
                return $"Dictionary<string, {valueTypeName}>";
            }
            return GetTypeName(type, context);
        }
        
        private string GetTypeName(TypeDef type, TypeContext context) {
            if (standardTypes.TryGetValue(type, out string name))
                return name;
            context.imports.Add(type);
            return type.Name;
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                string      filePath    = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Note}");
                sb.AppendLine("using System.Collections.Generic;");
                var namespaces = new HashSet<string>();
                foreach (var importPair in emitFile.imports) {
                    var import = importPair.Value;
                    if (import.type.Path == filePath)
                        continue;
                    if (customTypes.TryGetValue(import.type, out var @namespace)) {
                        namespaces.Add(@namespace);
                        continue;
                    }
                    namespaces.Add(import.@namespace);
                }
                foreach (var ns in namespaces) {
                    sb.AppendLine($"using {ns};");
                }
                sb.AppendLine();
                sb.AppendLine("#pragma warning disable 0169");
                sb.AppendLine();
                sb.AppendLine($"namespace {emitFile.package} {{");

                emitFile.header = sb.ToString();
            }
        }
        
        private void EmitFileFooters(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                sb.Clear();
                sb.AppendLine("}");
                emitFile.footer = sb.ToString();
            }
        }
    }
    
    internal readonly struct EmitField {
        internal readonly   string  type;
        internal readonly   string  name;
        internal readonly   bool    notNull;
        
        internal EmitField (string type, string name, bool notNull) {
            this.type       = type;
            this.name       = name;
            this.notNull   = notNull;
        }
    }
}