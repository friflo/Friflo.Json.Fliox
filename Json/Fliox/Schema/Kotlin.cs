// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Generator;
// Must not have other dependencies to Friflo.Json.Fliox.* except .Schema.Definition & .Schema.Utils

namespace Friflo.Json.Fliox.Schema
{
    public partial class KotlinGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  readonly   Dictionary<TypeDef, string> customTypes;

        private KotlinGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
            customTypes     = GetCustomTypes  (generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new KotlinGenerator(generator);
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
            // EmitFileFooters(sb);  no Kotlin footer
            generator.EmitFiles(sb, ns => $"{ns}{generator.fileExt}");
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.Boolean,       "Boolean" );
            AddType (map, standard.String,        "String" );

            AddType (map, standard.Uint8,         "Byte" );
            AddType (map, standard.Int16,         "Short" );
            AddType (map, standard.Int32,         "Int" );
            AddType (map, standard.Int64,         "Long" );
               
            AddType (map, standard.Double,        "Double" );
            AddType (map, standard.Float,         "Float" );
            AddType (map, standard.JsonKey,       "String" );
            return map;
        }
        
        private static Dictionary<TypeDef, string> GetCustomTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            AddType (map, standard.BigInteger,      "java.math" );
            AddType (map, standard.DateTime,        "kotlinx.datetime" );
            AddType (map, standard.Guid,            "java.util" );
            AddType (map, standard.JsonValue,       "kotlinx.serialization.json" );
            return map;
        }

        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            if (type.IsClass) {
                return EmitClassType(type, sb);
            }
            if (type.IsEnum) {
                var enumValues = type.EnumValues;
                sb.AppendLine($"enum class {type.Name} {{");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    {enumValue},");
                }
                sb.AppendLine("}");
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
            sb.AppendLine("@Serializable");
            if (type.Discriminant != null) {
                sb.AppendLine($"@SerialName(\"{type.Discriminant}\")");
            }
            // [inheritance - Extend data class in Kotlin - Stack Overflow] https://stackoverflow.com/questions/26444145/extend-data-class-in-kotlin
            if (!type.IsAbstract && baseType != null && baseType.IsAbstract) {
                extendsStr = $" : {baseType.Name}()";
                imports.Add(baseType);
            }
            var unionType = type.UnionType;
            if (unionType == null) {
                if (type.IsAbstract) {
                    // var baseClass = (baseType != null && baseType.IsAbstract) ? $": {baseType.Name}() " : "";
                    sb.AppendLine($"abstract class {type.Name} {{");
                } else {
                    var classType = type.Fields.Count == 0 ? "" : "data "; 
                    sb.AppendLine($"{classType}class {type.Name} (");
                }
            } else {
                // [Support multiple class discriminators for polymorphism. · Issue #546 · Kotlin/kotlinx.serialization] https://github.com/Kotlin/kotlinx.serialization/issues/546
                var baseClass = (baseType != null && baseType.IsAbstract) ? $": {baseType.Name}() " : "";
                sb.AppendLine($"// @JsonClassDiscriminator(\"{unionType.discriminator}\") https://github.com/Kotlin/kotlinx.serialization/issues/546");
                sb.AppendLine($"abstract class {type.Name}  {baseClass}{{");
            }
            foreach (var field in fields) {
                var @override =  field.IsDerivedField && type.BaseType.IsAbstract;
                var fieldModifier = type.IsAbstract ? "abstract " : @override ? "override " : "         ";
                var delimiter   = type.IsAbstract ? "" : ",";
                var required    = field.required;
                var fieldType   = GetFieldType(field, context);
                var indent      = Indent(maxFieldName, field.name);
                var nullable    = type.IsAbstract ? required ? "" : "?": required ? "": "? = null";
                if (field.type == context.standardTypes.BigInteger)
                    sb.AppendLine("              @Serializable(with = BigIntegerSerializer::class)");
                if (field.type == context.standardTypes.Guid)
                    sb.AppendLine("              @Serializable(with = UUIDSerializer::class)");
                sb.AppendLine($"    {fieldModifier} val {field.name}{indent} : {fieldType}{nullable}{delimiter}");
            }
            var closeBracket = type.IsAbstract ? "}" : ")";
            sb.AppendLine($"{closeBracket}{extendsStr}");
            sb.AppendLine();
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private string GetFieldType(FieldDef field, TypeContext context) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"List<{elementTypeName}>";
            }
            if (field.isDictionary) {
                var valueTypeName = GetElementType(field, context);
                return $"HashMap<String, {valueTypeName}>";
            }
            return GetTypeName(field.type, context);
        }
        
        private string GetElementType(FieldDef field, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, context);
            // if (field.isNullableElement)
            //    return $"{elementTypeName}?";
            return elementTypeName;
        }
        
        private string GetTypeName(TypeDef type, TypeContext context) {
            if (standardTypes.TryGetValue(type, out string name))
                return name;
            context.imports.Add(type);
            if (type == context.standardTypes.DateTime)
                return "Instant";
            if (type == context.standardTypes.JsonValue)
                return "JsonElement";
            if (type == context.standardTypes.Guid)
                return "UUID";
            return type.Name;
        }
        
        private void EmitFileHeaders(StringBuilder sb) {
            foreach (var pair in generator.fileEmits) {
                EmitFile    emitFile    = pair.Value;
                string      filePath    = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Note}");
                sb.AppendLine($"package {emitFile.@namespace}");
                sb.AppendLine();
                sb.AppendLine("import kotlinx.serialization.*");
                sb.AppendLine("import CustomSerializer.*");
                
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
                    sb.AppendLine($"import {ns}.*");
                }
                emitFile.header = sb.ToString();
            }
        }
    }
}