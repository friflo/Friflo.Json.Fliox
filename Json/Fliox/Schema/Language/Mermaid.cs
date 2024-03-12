// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Doc;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class MermaidClassDiagramGenerator
    {
        private  readonly   Generator                   generator;
        private  readonly   Dictionary<TypeDef, string> standardTypes;
        private  const      string                      Union = "";

        private MermaidClassDiagramGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new MermaidClassDiagramGenerator(generator);
            var sb      = new StringBuilder();
            foreach (var type in generator.types) {
                sb.Clear();
                var result = emitter.EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPath(true); // sort dependencies - otherwise possible error TS2449: Class '...' used before its declaration.

            EmitMermaidERFile(generator, sb);
        }
        
        private static Dictionary<TypeDef, string> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, string>();
            var nl= '\n'; // not Environment.NewLine;
            
            AddType (map, standard.Uint8,       $"/** unsigned integer 8-bit. Range: [0 - 255]                                  */{nl}export type uint8 = number" );
            AddType (map, standard.Int16,       $"/** signed integer 16-bit. Range: [-32768, 32767]                             */{nl}export type int16 = number" );
            AddType (map, standard.Int32,       $"/** signed integer 32-bit. Range: [-2147483648, 2147483647]                   */{nl}export type int32 = number" );
            AddType (map, standard.Int64,       $"/** signed integer 64-bit. Range: [-9223372036854775808, 9223372036854775807]{nl}" +
                                                $" *  number in JavaScript.  Range: [-9007199254740991, 9007199254740991]       */{nl}export type int64 = number" );
            
            // NON_CLS
            AddType (map, standard.Int8,        $"/** unsigned integer 8-bit. Range: [–128 - 127]                               */{nl}export type uint8 = number" );
            AddType (map, standard.UInt16,      $"/** signed integer 16-bit. Range: [0, 65535]                                  */{nl}export type int16 = number" );
            AddType (map, standard.UInt32,      $"/** signed integer 32-bit. Range: [0, 4294967295]                             */{nl}export type int32 = number" );
            AddType (map, standard.UInt64,      $"/** signed integer 64-bit. Range: [0, 18446744073709551615]{nl}" +
                                                $" *  number in JavaScript.  Range: [0, 9007199254740991]                       */{nl}export type int64 = number" );
               
            AddType (map, standard.Double,      $"/** double precision floating point number */{nl}export type double = number" );
            AddType (map, standard.Float,       $"/** single precision floating point number */{nl}export type float = number" );
               
            AddType (map, standard.BigInteger,  $"/** integer with arbitrary precision       */{nl}export type BigInteger = string" );
            AddType (map, standard.DateTime,    $"/** timestamp as RFC 3339 + milliseconds   */{nl}export type DateTime = string" );
            AddType (map, standard.Guid,        $"/** GUID / UUID as RFC 4122. e.g. \"123e4567-e89b-12d3-a456-426614174000\" */{nl}export type Guid = string" );
            AddType (map, standard.String,      "/** string **/" );
            AddType (map, standard.Boolean,     "/** boolean **/" );
            AddType (map, standard.JsonValue,   "/** any **/" );
            AddType (map, standard.JsonKey,     "/** string **/" );
            AddType (map, standard.ShortString, "/** string **/" );
            AddType (map, standard.JsonEntity,  "/** any **/" );
            return map;
        }

        private EmitType EmitStandardType(TypeDef type, StringBuilder sb) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            sb.Append(definition);
            sb.AppendLF(";");
            sb.AppendLF();
            return new EmitType(type, sb);
        }
        
        private EmitType EmitType(TypeDef type, StringBuilder sb) {
            var standardType    = EmitStandardType(type, sb);
            if (standardType != null ) {
                return null;
            }
            if (type.IsClass) {
                return EmitClassType(type, sb);
            }
            if (type.IsEnum) {
                var enumValues  = type.EnumValues;
                // var doc         = GetDoc(type.doc, "");
                sb.AppendLF($"class {type.Name}:::cssEnum {{");
                sb.AppendLF("    <<enumeration>>");
                foreach (var enumValue in enumValues) {
                    sb.AppendLF($"    {enumValue.name}");
                }
                sb.AppendLF("}");
                sb.AppendLF();
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
            // var doc             = GetDoc(type.doc, "");
            if (baseType != null) {
                dependencies.Add(baseType);
                imports.Add(baseType);
                sb.AppendLF($"{baseType.Name} <|-- {type.Name}");
            }
            var unionType = type.UnionType;
            var cssType = type.IsEntity ? ":::cssEntity" : type.IsSchema ? ":::cssSchema" : "";
            if (unionType == null) {
                sb.AppendLF($"class {type.Name}{cssType} {{");
                if (type.IsSchema)      sb.AppendLF("    <<Schema>>");
                if (type.IsAbstract)    sb.AppendLF("    <<abstract>>");
                if (type.IsEntity)      sb.AppendLF($"    <<Entity · {type.KeyField}>>");
            } else {
                sb.AppendLF($"class {type.Name}{Union}{cssType} {{");
                sb.AppendLF("    <<abstract>>");
            }
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                // sb.Append(GetDoc(type.DiscriminatorDoc, "    "));
                sb.AppendLF($"    {discriminator}{indent}  : \"{discriminant}\"");
            }
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                // var fieldDoc    = GetDoc(field.doc, "    ");
                bool required   = field.required;
                var fieldType   = GetFieldType(field, context, true); // required);
                var indent      = Indent(maxFieldName, field.name);
                var optStr      = required ? " ": "?";
                sb.AppendLF($"    {field.name}{optStr}{indent} : {fieldType}");
            }
            sb.AppendLF("}");
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                // var fieldDoc    = GetDoc(field.doc, "    ");
                var fieldType   = field.type;
                var cardinality = GetFieldCardinality(field);
                if (!standardTypes.ContainsKey(fieldType)) {
                    // ◆⎯⎯⎯  Relationship: Composition - the lifetime of item(s) is bound to its owner instance
                    sb.AppendLF($"{type.Name} *-- \"{cardinality}\" {fieldType.Name} : {field.name}");
                }
                var relationType = field.RelationType;
                if (relationType != null) {
                    // ◇⎯⎯⎯  Relationship: Aggregation - the lifetime of referenced entities are independent from its owner instance
                    sb.AppendLF($"{type.Name} o.. \"{cardinality}\" {relationType.Name} : {field.name}");
                }
            }
            if (EmitMessagesFlag && type.IsSchema) {
                sb.AppendLF($"{type.Name} ..> Messages");
                EmitMessages(type.Commands, context, sb);
                EmitMessages(type.Messages, context, sb);
            }
            return new EmitType(type, sb, imports, dependencies);
        }
        
        private void EmitMessages(IReadOnlyList<MessageDef> messageDefs, TypeContext context, StringBuilder sb) {
            if (messageDefs == null)
                return;
            sb.AppendLF();
            sb.AppendLF("class Messages:::cssSchema {");
            sb.AppendLF("    <<Service>>");
            int maxFieldName    = messageDefs.MaxLength(field => field.name.Length + 4); // 4 <= ["..."]
            foreach (var messageDef in messageDefs) {
                var param   = GetMessageArg("param", messageDef.param,  context);
                var result  = GetMessageArg(null,    messageDef.result, context);
                // var doc     = GetDoc(messageDef.doc, "    ");
                // sb.Append(doc);
                var indent  = Indent(maxFieldName, messageDef.name);
                var signature = $"({param}) {result ?? "void"}";
                var name    = messageDef.name.Replace('.', '_');
                sb.AppendLF($"    {name}{indent} {signature}");
            }
            sb.AppendLF("}");
            foreach (var messageDef in messageDefs) {
                Link(messageDef.name, messageDef.param,   sb);
                Link(messageDef.name, messageDef.result,  sb);
            }
        }

        // ReSharper disable once ConvertToConstant.Local
        private static readonly bool EmitMessagesFlag = false;

        private static string GetMessageArg(string name, FieldDef fieldDef, TypeContext context) {
            if (fieldDef == null)
                return name != null ? "" : "void";
            var argType = GetFieldType(fieldDef, context, fieldDef.required);
            return name != null ? $"{name}: {argType}" : argType;
        }
        
        private void Link(string name, FieldDef fieldDef, StringBuilder sb) {
            if (fieldDef == null)
                return;
            var fieldType = fieldDef.type;
            if (standardTypes.ContainsKey(fieldType))
                return;
            sb.AppendLF($"Messages ..> {fieldType.Name} : {name}");
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context, bool required) {
            var nullStr = required ? "" : " | null";
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"{elementTypeName}[]{nullStr}";
            }
            if (field.isDictionary) {
                var key = field.type.IsEntity ? $"[{field.type.KeyField}]" : "string";
                var valueTypeName = GetElementType(field, context);
                return $"{key} ➞ {valueTypeName}{nullStr}";
                // return $"{valueTypeName}[]{nullStr}";
            }
            return $"{GetTypeName(field.type, context)}{nullStr}";
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
                return "any"; // known as Mr anti-any  :) 
            if (type == standard.JsonEntity)
                return "any"; 
            if (type == standard.String || type == standard.JsonKey || type == standard.ShortString)
                return "string";
            if (type == standard.Boolean)
                return "boolean";
            context.imports.Add(type);
            if (type.UnionType != null)
                return $"{type.Name}{Union}";
            return type.Name;
        }
        
        private static string GetFieldCardinality(FieldDef field) {
            if (field.isArray || field.isDictionary) {
                return "0..*";
            }
            return field.required ? "1" : "0..1";
        }
        
        // ReSharper disable once UnusedMember.Local
        private static string GetDoc(string docs, string indent) {
            return TypeDoc.HtmlToDoc(docs, indent, "/**", " * ", " */");
        }
        
        private static void EmitMermaidERFile(Generator generator, StringBuilder sb) {
            sb.Clear();
            sb.AppendLF("classDiagram");
            sb.AppendLF("direction LR");
            sb.AppendLF();
            var fileEmits = generator.OrderNamespaces();
            
            var dependencies = new HashSet<TypeDef>();
            var rootType = generator.rootType;
            if (!EmitMessagesFlag && rootType != null) {
                rootType.GetDependencies(dependencies);
            } else {
                foreach (var type in generator.types) {
                    type.GetDependencies(dependencies);
                }
            }
            foreach (var emitFile in fileEmits) {
                // string ns = emitFile.@namespace;
                foreach (var result in emitFile.emitTypes) {
                    if (!dependencies.Contains(result.type))
                        continue;
                    sb.AppendLF(result.content);
                }
            }
            var mermaidFile     = sb.ToString();
            generator.files.Add("class-diagram.mmd", mermaidFile);
        }
    }
}