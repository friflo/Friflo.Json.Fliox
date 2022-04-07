// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Doc;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils, .Schema.GraphQL
namespace Friflo.Json.Fliox.Schema.Language
{
    internal class EmitTypeGql : EmitType {
        internal readonly   GqlType graphQLType;
        
        public EmitTypeGql(
            TypeDef             type,
            GqlType             graphQLType,
            HashSet<TypeDef>    imports = null,
            List<TypeDef>       dependencies = null) : base(type, imports, dependencies)
        {
            this.graphQLType    = graphQLType;
        }
    }
    
    public sealed partial class GraphQLGenerator
    {
        private  readonly   Generator                       generator;
        private  readonly   Dictionary<TypeDef, GqlType>    standardTypes;
        private  const      string                          Union = "_Union";
        
        private GraphQLGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static void Generate(Generator generator) {
            var emitter = new GraphQLGenerator(generator);
            var types   = new List<GqlType>();
            foreach (var type in generator.types) {
                var result = emitter.EmitType(type);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
                types.Add(result.graphQLType);
            }
            var schema = new GqlSchema {
                queryType   = new GqlType { name = "Query" },
                types       = types,
                directives  = new List<GqlDirective>()
            };
            
            using (var typeStore    = new TypeStore()) 
            using (var writer       = new ObjectWriter(typeStore)) {
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var schemaJson          = writer.Write(schema);
                generator.files.Add("schema.json", schemaJson);
            }
        }
        
        private static void AddType (Dictionary<TypeDef, GqlType> types, TypeDef type, GqlType value, string description) {
            if (type == null)
                return;
            types.Add(type, value);
        }
        
        private static Dictionary<TypeDef, GqlType> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, GqlType>();
            var nl  = Environment.NewLine;
            AddType (map, standard.Uint8,       Gql.Int(),      $"unsigned integer 8-bit. Range: [0 - 255]" );
            AddType (map, standard.Int16,       Gql.Int(),      $"signed integer 16-bit. Range: [-32768, 32767]" );
            AddType (map, standard.Int32,       Gql.Int(),      $"signed integer 32-bit. Range: [-2147483648, 2147483647]" );
            AddType (map, standard.Int64,       Gql.Int(),      $"signed integer 64-bit. Range: [-9223372036854775808, 9223372036854775807]{nl}" +
                                                                $"number in JavaScript.  Range: [-9007199254740991, 9007199254740991]" );
               
            AddType (map, standard.Double,      Gql.Float(),    $"double precision floating point number" );
            AddType (map, standard.Float,       Gql.Float(),    $"single precision floating point number" );
               
            AddType (map, standard.BigInteger,  Gql.String(),   $"integer with arbitrary precision" );
            AddType (map, standard.DateTime,    Gql.String(),   $"timestamp as RFC 3339 + milliseconds" );
            AddType (map, standard.Guid,        Gql.String(),   $"GUID / UUID as RFC 4122. e.g. \"123e4567-e89b-12d3-a456-426614174000\"" );
            return map;
        }
        
        private EmitTypeGql EmitStandardType(TypeDef type) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            return new EmitTypeGql(type, definition);
        }
        
        private EmitTypeGql EmitType(TypeDef type) {
            var standardType    = EmitStandardType(type);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsClass) {
                return EmitClassType(type);
            }
            if (type.IsEnum) {
                return null;
            /*  var enumValues  = type.EnumValues;
                var doc         = GetDoc(type.doc, "");
                var maxNameLen  = enumValues.Max(e => e.name.Length);
                sb.AppendLine($"{doc}export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.Append($"    | \"{enumValue.name}\"");
                    var enumDoc = enumValue.doc;
                    if (enumDoc != null) {
                        sb.Append(Indent(maxNameLen, enumValue.name));
                        sb.Append(GetDoc(enumDoc, "      "));
                        continue;
                    }
                    sb.AppendLine();
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(type, sb); */
            }
            return null;
        }
        
        private EmitTypeGql EmitClassType(TypeDef type) {
            var obj             = new GqlObject();
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var dependencies    = new List<TypeDef>();
            var fields          = type.Fields;
            int maxFieldName    = fields.MaxLength(field => field.name.Length);
            var extendsStr      = "";
            var baseType        = type.BaseType;
            var doc             = GetDoc(type.doc, "");
            obj.description = doc;
            if (baseType != null) {
                extendsStr = $"extends {baseType.Name} ";
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            var unionType = type.UnionType;
        /*    if (unionType == null) {
                if (type.IsSchema) sb.AppendLine("// schema documentation only - not implemented right now");
                var typeName = type.IsSchema ? "interface" : type.IsAbstract ? "abstract class" : "class";
                sb.AppendLine($"export {typeName} {type.Name} {extendsStr}{{");
                if (type.IsSchema)
                    sb.AppendLine("    // --- containers");
            } else {
                sb.AppendLine($"export type {type.Name}{Union} =");
                foreach (var polyType in unionType.types) {
                    var polyTypeDef = polyType.typeDef;
                    sb.AppendLine($"    | {polyTypeDef.Name}");
                    imports.Add(polyTypeDef);
                }
                var fieldDoc    = GetDoc(unionType.doc, "    ");
                sb.AppendLine($";");
                sb.AppendLine();
                sb.AppendLine($"export abstract class {type.Name} {extendsStr}{{");
                sb.Append(fieldDoc);
                sb.AppendLine($"    abstract {unionType.discriminator}:");
                foreach (var polyType in unionType.types) {
                    sb.AppendLine($"        | \"{polyType.discriminant}\"");
                }
                sb.AppendLine($"    ;");
            }
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                sb.Append(GetDoc(type.DiscriminatorDoc, "    "));
                sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
            }
            foreach (var field in fields) {
                if (field.IsDerivedField)
                    continue;
                var fieldDoc    = GetDoc(field.doc, "    ");
                sb.Append(fieldDoc);
                bool required   = field.required;
                var fieldType   = GetFieldType(field, context, required);
                var indent      = Indent(maxFieldName, field.name);
                var optStr      = required ? " ": "?";
                sb.AppendLine($"    {field.name}{optStr}{indent} : {fieldType};");
            }
            EmitMessages("commands", type.Commands, context, sb);
            EmitMessages("messages", type.Messages, context, sb);

            sb.AppendLine("}");
            sb.AppendLine(); */
            return new EmitTypeGql(type, obj, imports, dependencies);
        }
        
        private static void EmitMessages(string type, IReadOnlyList<MessageDef> messageDefs, TypeContext context, StringBuilder sb) {
            if (messageDefs == null)
                return;
            sb.AppendLine($"\n    // --- {type}");
            int maxFieldName    = messageDefs.MaxLength(field => field.name.Length + 4); // 4 <= ["..."]
            foreach (var messageDef in messageDefs) {
                var param   = GetMessageArg("param", messageDef.param,  context);
                var result  = GetMessageArg(null,    messageDef.result, context);
                var doc     = GetDoc(messageDef.doc, "    ");
                sb.Append(doc);
                var indent  = Indent(maxFieldName, messageDef.name);
                var signature = $"({param}) : {result ?? "void"}";
                sb.AppendLine($"    [\"{messageDef.name}\"]{indent} {signature};");
            }
        }
        
        private static string GetMessageArg(string name, FieldDef fieldDef, TypeContext context) {
            if (fieldDef == null)
                return name != null ? "" : "void";
            var argType = GetFieldType(fieldDef, context, fieldDef.required);
            return name != null ? $"{name}: {argType}" : argType;
        }
        
        private static string GetFieldType(FieldDef field, TypeContext context, bool required) {
            var nullStr = required ? "" : " | null";
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                return $"{elementTypeName}[]{nullStr}";
            }
            if (field.isDictionary) {
                var valueTypeName = GetElementType(field, context);
                return $"{{ [key: string]: {valueTypeName} }}{nullStr}";
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
            if (type == standard.String || type == standard.JsonKey)
                return "string";
            if (type == standard.Boolean)
                return "boolean";
            context.imports.Add(type);
            if (type.UnionType != null)
                return $"{type.Name}{Union}";
            return type.Name;
        }
        
        private static string GetDoc(string docs, string indent) {
            return TypeDoc.HtmlToDoc(docs, indent, "/**", " * ", " */");
        }
    }
}