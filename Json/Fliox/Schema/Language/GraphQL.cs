// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        
        public static GqlSchema Generate(Generator generator) {
            var emitter     = new GraphQLGenerator(generator);
            var schemaType  = generator.FindSchemaType();
            var queries     = CreateQueries(schemaType);
            var types   = new List<GqlType> {
                Gql.String(),
                Gql.Int(),
                Gql.Float(),
                Gql.Boolean(),
                Gql.Any(),
                new GqlObject { name = "Query", fields = queries }
            };
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
            var graphQLSchema = CreateSchema(schema);
            generator.files.Add("schema.graphql", graphQLSchema);

            return schema;
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
                var enumType            = new GqlEnum { enumValues = new List<GqlEnumValue>() };
                var enumValues          = type.EnumValues;
                enumType.description    = GetDoc(type.doc, "");
                enumType.name           = type.Name;
                foreach (var enumValue in enumValues) {
                    var gqlValue = new GqlEnumValue {
                        name        = enumValue.name,
                        description = GetDoc(enumValue.doc, "")
                    };
                    enumType.enumValues.Add(gqlValue);
                }
                return new EmitTypeGql(type, enumType);
            }
            return null;
        }
        
        private EmitTypeGql EmitClassType(TypeDef type) {
            var gqlFields       = new List<GqlField>();
            var obj             = new GqlObject { fields = gqlFields };
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var dependencies    = new List<TypeDef>();
            var fields          = type.Fields;
            var extendsStr      = "";
            var baseType        = type.BaseType;
            var doc             = GetDoc(type.doc, "");
            obj.description = doc;
            if (baseType != null) {
                extendsStr = $"extends {baseType.Name} ";
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            obj.name    = type.Name;
            var unionType = type.UnionType;
            if (unionType == null) {
                // if (type.IsSchema) sb.AppendLine("// schema documentation only - not implemented right now");
                var typeName = type.IsSchema ? "interface" : type.IsAbstract ? "abstract class" : "class";
                // sb.AppendLine($"export {typeName} {type.Name} {extendsStr}{{");
            } else {
                /*  sb.AppendLine($"export type {type.Name}{Union} =");
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
                sb.AppendLine($"    ;"); */
            }
            string  discriminant    = type.Discriminant;
            string  discriminator   = type.Discriminator;
            /* if (discriminant != null) {
                maxFieldName    = Math.Max(maxFieldName, discriminator.Length);
                var indent      = Indent(maxFieldName, discriminator);
                sb.Append(GetDoc(type.DiscriminatorDoc, "    "));
                sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
            } */
            foreach (var field in fields) {
                var gqlField = new GqlField();
                gqlFields.Add(gqlField);
                //  if (field.IsDerivedField)
                //      continue;
                gqlField.description    = GetDoc(field.doc, "    ");
                gqlField.name           = field.name;
                bool required           = field.required;
                gqlField.type           = GetFieldType(field, context, required);
            }
            // EmitMessages("commands", type.Commands, context, sb);
            // EmitMessages("messages", type.Messages, context, sb);

            return new EmitTypeGql(type, obj, imports, dependencies);
        }
        
        private static List<GqlField> EmitMessages(string type, IReadOnlyList<MessageDef> messageDefs, TypeContext context) {
            if (messageDefs == null)
                return null;
            var fields = new List<GqlField>();
            foreach (var messageDef in messageDefs) {
                var field           = new GqlField { name = messageDef.name, args = new List<GqlInputValue>() }; 
                var param           = GetMessageArg("param", messageDef.param,  context);
                field.args.Add(param);
                var result          = messageDef.result;
                if (result != null) {
                    field.type      = GetFieldType(result, context, result.required);
                }
                field.description   = GetDoc(messageDef.doc, "    ");
            }
            return fields;
        }
        
        private static GqlInputValue GetMessageArg(string name, FieldDef fieldDef, TypeContext context) {
            if (fieldDef == null)
                return null;
            var argType = GetFieldType(fieldDef, context, fieldDef.required);
            return new GqlInputValue { type = argType, name = name };
        }
        
        private static GqlType GetFieldType(FieldDef field, TypeContext context, bool required) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, context);
                var listType = new GqlList { ofType = elementTypeName };
                return Gql.Type(listType, required);
            }
            if (field.isDictionary) {
                // var valueTypeName = GetElementType(field, context);
                // return $"{{ [key: string]: {valueTypeName} }}{nullStr}";
                return Gql.Any();
            }
            var type = GetTypeName(field.type, context);
            return Gql.Type(type, required);
        }
        
        private static GqlType GetElementType(FieldDef field, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, context);
            return Gql.Type(elementTypeName, !field.isNullableElement);
        }
        
        private static GqlType GetTypeName(TypeDef type, TypeContext context) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return Gql.Any();
            if (type == standard.String   || type == standard.JsonKey ||
                type == standard.DateTime || type == standard.Guid    || type == standard.BigInteger )
                return Gql.String();
            if (type == standard.Boolean)
                return Gql.Boolean();
            if (type == standard.Float || type == standard.Double)
                return Gql.Float();
            if (type == standard.Uint8 || type == standard.Int16 || type == standard.Int32|| type == standard.Int64)
                return Gql.Int();
            context.imports.Add(type);
            if (type.UnionType != null)
                return new GqlScalar { name = type.Name };
            return new GqlScalar { name = type.Name };
        }
        
        // todo remove indent and Typescript comment syntax
        private static string GetDoc(string docs, string indent) {
            return TypeDoc.HtmlToDoc(docs, indent, "/**", " * ", " */");
        }
        
        private static List<GqlField> CreateQueries(TypeDef schemaType) {
            var queries     = new List<GqlField>();
            foreach (var field in schemaType.Fields) {
                var containerType = Gql.Scalar(field.type.Name);
                var query = new GqlField { name = field.name,
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("filter",   Gql.String()),
                        Gql.InputValue ("limit",    Gql.Int())
                    },
                    type = Gql.List(containerType, true, true)
                };
                var queryById = new GqlField { name = $"{field.name}ById",
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("ids",      Gql.List(Gql.String(), true, true))
                    },
                    type = Gql.List(containerType, true, false)
                };
                queries.Add(query);
                queries.Add(queryById);
            }
            return queries;
        }
    }
}