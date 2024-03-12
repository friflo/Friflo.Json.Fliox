// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Doc;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Schema.Utils;


// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils, .Schema.GraphQL
namespace Friflo.Json.Fliox.Schema.Language
{
    internal sealed class EmitTypeGql : EmitType {
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
        
        private GraphQLGenerator (Generator generator) {
            this.generator  = generator;
            standardTypes   = GetStandardTypes(generator.standardTypes);
        }
        
        public static GqlSchema Generate(Generator generator) {
            var emitter     = new GraphQLGenerator(generator);
            var schemaType  = generator.FindSchemaType();
            var queries     = CreateQueries  (schemaType, generator);
            var mutations   = CreateMutations(schemaType);
            var query       = Gql.Object("Query", queries);
            var types   = new List<GqlType> {
                Gql.String(),
                Gql.Int(),
                Gql.Float(),
                Gql.Boolean(),
                Gql.Any(),
                Gql.SortOrder(),
                query
            };
            CreateResultTypes (types, schemaType);
            if (mutations != null) {
                var mutation = Gql.Object("Mutation", mutations);
                types.Add(mutation);
            }
            foreach (var type in generator.types) {
                var result = emitter.EmitType(type, Kind.Output);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
                types.Add(result.graphQLType);
                var isClass = type.IsClass && !type.IsSchema;
                if (isClass) {
                    result = emitter.EmitClassType(type, Kind.Input);
                    types.Add(result.graphQLType);
                }
            }
            var schema = new GqlSchema {
                queryType       = new GqlType { name = "Query" },
                mutationType    = mutations != null ? new GqlType { name = "Mutation" } : null,
                types           = types,
                directives      = new List<GqlDirective>()
            };
            var graphQLSchema = CreateSchema(schema, schemaType?.schemaInfo);
            generator.files.Add("schema.graphql", graphQLSchema);

            return schema;
        }
        
        private static void AddType (Dictionary<TypeDef, GqlType> types, TypeDef type, GqlType value, string description) {
            if (type == null)
                return;
            value.description = description;
            types.Add(type, value);
        }
        
        private static Dictionary<TypeDef, GqlType> GetStandardTypes(StandardTypes standard) {
            var map = new Dictionary<TypeDef, GqlType>();
            var nl= '\n'; // not Environment.NewLine;
            AddType (map, standard.Uint8,       Gql.Int(),      $"unsigned integer 8-bit. Range: [0 - 255]" );
            AddType (map, standard.Int16,       Gql.Int(),      $"signed integer 16-bit. Range: [-32768, 32767]" );
            AddType (map, standard.Int32,       Gql.Int(),      $"signed integer 32-bit. Range: [-2147483648, 2147483647]" );
            AddType (map, standard.Int64,       Gql.Int(),      $"signed integer 64-bit. Range: [-9223372036854775808, 9223372036854775807]{nl}" +
                                                                $"number in JavaScript.  Range: [-9007199254740991, 9007199254740991]" );
            // NON_CLS
            AddType (map, standard.Int8,        Gql.Int(),     $"signed integer 8-bit. Range: [-128 - 127]" );
            AddType (map, standard.UInt16,      Gql.Int(),     $"unsigned integer 16-bit. Range: [0, 65535]" );
            AddType (map, standard.UInt32,      Gql.Int(),     $"unsigned integer 32-bit. Range: [0, 4294967295]" );
            AddType (map, standard.UInt64,      Gql.Int(),     $"unsigned integer 64-bit. Range: [0, 18446744073709551615]{nl}" +
                                                               $"number in JavaScript.  Range: [0, 9007199254740991]" );
               
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
        
        private EmitTypeGql EmitType(TypeDef type, Kind kind) {
            var standardType    = EmitStandardType(type);
            if (standardType != null ) {
                return null;
            }
            if (type.IsClass) {
                return EmitClassType(type, kind);
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
        
        private EmitTypeGql EmitClassType(TypeDef type, Kind kind) {
            GqlType     gqlType;
            var gqlFields       = new List<GqlField>();
            var imports         = new HashSet<TypeDef>();
            var context         = new TypeContext (generator, imports, type);
            var dependencies    = new List<TypeDef>();
            var fields          = type.Fields;
            var baseType        = type.BaseType;
            var doc             = GetDoc(type.doc, "");
            if (baseType != null) {
                // extendsStr = $"extends {baseType.Name} ";
                dependencies.Add(baseType);
                imports.Add(baseType);
            }
            
            var unionType = type.UnionType;
            if (unionType == null) {
                if (kind == Kind.Output) {
                    gqlType = Gql.Object(null, gqlFields);
                } else {
                    gqlType = new GqlInputObject { inputFields  = gqlFields };
                }
                // var typeName = type.IsSchema ? "interface" : type.IsAbstract ? "abstract class" : "class";
                // sb.AppendLF($"export {typeName} {type.Name} {extendsStr}{{");
            } else {
                if (kind == Kind.Output) { 
                    var union   = new GqlUnion { possibleTypes = new List<GqlType>() };
                    gqlType     = union;
                    // var fieldDoc    = GetDoc(unionType.doc, "    ");
                    // sb.AppendLF($"    abstract {unionType.discriminator}:");
                    foreach (var polyType in unionType.types) {
                        var unionItemType = Gql.Scalar(polyType.typeDef.Name);
                        union.possibleTypes.Add(unionItemType);
                    }
                } else {
                    gqlType = Gql.Any(); // todo - need to check the agreed solution for input union types. options:
                    // [RFC: OneOf Input Objects by benjie · Pull Request #825 · graphql/graphql-spec] https://github.com/graphql/graphql-spec/pull/825
                }
            }
            gqlType.name            = GetName(type, kind);
            gqlType.description     = doc;
            
            foreach (var field in fields) {
                var gqlField = new GqlField();
                gqlFields.Add(gqlField);
                //  if (field.IsDerivedField)
                //      continue;
                gqlField.description    = GetDoc(field.doc, "    ");
                gqlField.name           = field.name;
                bool required           = field.required;
                gqlField.type           = GetFieldType(field, context, required, kind);
            }
            return new EmitTypeGql(type, gqlType, imports, dependencies);
        }
        
        private static void EmitMessages(List<GqlField> fields, IReadOnlyList<MessageDef> messageDefs, TypeContext context) {
            if (messageDefs == null)
                return;
            foreach (var messageDef in messageDefs) {
                var name            = messageDef.name.Replace(".", "_");
                var field           = new GqlField { name = name, args = new List<GqlInputValue>() }; 
                if (messageDef.param != null) {
                    var param       = GetMessageArg("param", messageDef.param, Kind.Input,  context);
                    field.args.Add(param);
                }
                var result          = messageDef.result;
                if (result != null) {
                    field.type      = GetFieldType(result, context, result.required, Kind.Output);
                } else {
                    field.type      = Gql.Any(); // todo - check returning error for messages
                }
                field.description   = GetDoc(messageDef.doc, "");
                fields.Add(field);
            }
        }
        
        private static GqlInputValue GetMessageArg(string name, FieldDef fieldDef, Kind kind, TypeContext context) {
            if (fieldDef == null)
                return null;
            var argType = GetFieldType(fieldDef, context, fieldDef.required, kind);
            return new GqlInputValue { type = argType, name = name };
        }
        
        private static GqlType GetFieldType(FieldDef field, TypeContext context, bool required, Kind kind) {
            if (field.isArray) {
                var elementTypeName = GetElementType(field, kind, context);
                var listType = new GqlList { ofType = elementTypeName };
                return Gql.Type(listType, required);
            }
            if (field.isDictionary) {
                // var valueTypeName = GetElementType(field, context);
                // return $"{{ [key: string]: {valueTypeName} }}{nullStr}";
                return Gql.Any();
            }
            var type = GetTypeName(field.type, kind, context);
            return Gql.Type(type, required);
        }
        
        private static GqlType GetElementType(FieldDef field, Kind kind, TypeContext context) {
            var elementTypeName = GetTypeName(field.type, kind, context);
            return Gql.Type(elementTypeName, !field.isNullableElement);
        }
        
        private static GqlType GetTypeName(TypeDef type, Kind kind, TypeContext context) {
            var standard = context.standardTypes;
            if (type == standard.JsonValue)
                return Gql.Any();
            if (type == standard.JsonEntity)
                return Gql.Any();
            if (type == standard.JsonTable)
                return Gql.Table();
            if (type == standard.String   || type == standard.JsonKey || type == standard.ShortString ||
                type == standard.DateTime || type == standard.Guid    || type == standard.BigInteger )
                return Gql.String();
            if (type == standard.Boolean)
                return Gql.Boolean();
            if (type == standard.Float || type == standard.Double)
                return Gql.Float();
            if (type == standard.Uint8 || type == standard.Int16  || type == standard.Int32  || type == standard.Int64 ||
                type == standard.Int8  || type == standard.UInt16 || type == standard.UInt32 || type == standard.UInt64)
                return Gql.Int();
            context.imports.Add(type);
            var name = GetName(type, kind);
            if (type.UnionType != null)
                return Gql.Scalar(name);
            return Gql.Scalar(name);
        }
        
        // todo remove indent and Typescript comment syntax
        private static string GetDoc(string docs, string indent) {
            return TypeDoc.HtmlToDoc(docs, indent, "/**", " * ", " */");
        }
        
        private static List<GqlField> CreateQueries(TypeDef schemaType, Generator generator) {
            var queries     = new List<GqlField>();
            if (schemaType == null)
                return queries;
            var fields = schemaType.Fields;
            foreach (var field in fields) {
                var resultType = Gql.MethodResult("query", field.name);
                var query = new GqlField {
                    name = Gql.MethodName("query", field.name),
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("filter",       Gql.String()),
                        Gql.InputValue ("limit",        Gql.Int()),
                        Gql.InputValue ("maxCount",     Gql.Int()),
                        Gql.InputValue ("cursor",       Gql.String()),
                        Gql.InputValue ("selectAll",    Gql.Boolean()),
                        Gql.InputValue ("orderByKey",   Gql.SortOrder())
                    },
                    type = Gql.Type(Gql.Scalar(resultType), true)
                };
                queries.Add(query);
            }
            foreach (var field in fields) {
                var containerType = Gql.Scalar(field.type.Name);
                var queryById = new GqlField {
                    name = Gql.MethodName("read", field.name),
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("ids",          Gql.List(Gql.String(), true, true)),
                        Gql.InputValue ("selectAll",    Gql.Boolean())
                    },
                    type = Gql.List(containerType, true, false)
                };
                queries.Add(queryById);
            }
            foreach (var field in fields) {
                var count = new GqlField {
                    name = Gql.MethodName("count", field.name),
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("filter",   Gql.String()),
                    },
                    type = Gql.Int()
                };
                queries.Add(count);
            }
            var imports = new HashSet<TypeDef>();
            var context = new TypeContext (generator, imports, schemaType);
            EmitMessages(queries, schemaType.Commands, context);
            EmitMessages(queries, schemaType.Messages, context);
            return queries;
        }
        
        private static List<GqlField> CreateMutations(TypeDef schemaType) {
            if (schemaType == null)
                return null;
            var mutations   = new List<GqlField>();
            AddMutations("create", mutations, schemaType);
            AddMutations("upsert", mutations, schemaType);
            foreach (var field in schemaType.Fields) {
                var query = new GqlField {
                    name = Gql.MethodName("delete", field.name),
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("ids",   Gql.List(Gql.String(), true, true)),
                    },
                    type = Gql.List(Gql.Scalar("EntityError"), false, true)
                };
                mutations.Add(query);
            }
            return mutations;
        }
        
        private static void AddMutations(string methodType, List<GqlField> mutations, TypeDef schemaType) {
            foreach (var field in schemaType.Fields) {
                var containerType   = Gql.ScalarInput(field.type.Name);
                var list            = Gql.List(containerType, true, true);
                var query = new GqlField {
                    name = Gql.MethodName(methodType, field.name),
                    args = new List<GqlInputValue> {
                        Gql.InputValue ("entities",   list),
                    },
                    type = Gql.List(Gql.Scalar("EntityError"), false, true)
                };
                mutations.Add(query);
            }
        }
        
        private static string GetName (TypeDef type, Kind kind) {
            var name = type.Name;
            if (kind == Kind.Input && !type.IsEnum)
                return name + "Input";
            return name;
        }
        
        private static void CreateResultTypes(List<GqlType> types, TypeDef schemaType) {
            if (schemaType == null)
                return;
            var fields = schemaType.Fields;
            foreach (var field in fields) {
                var resultType      = Gql.MethodResult("query", field.name);
                var containerType   = Gql.Scalar(field.type.Name);
                var count           = Gql.Field("count",    Gql.Type(Gql.Int(), true));
                var cursor          = Gql.Field("cursor",   Gql.String());
                var items           = Gql.Field("items",    Gql.List(containerType, true, true));
                var resultFields    = new List<GqlField> { count, cursor, items };
                var type            = Gql.Object(resultType, resultFields);
                types.Add(type);
            }
            var entityErrorType = EntityErrorType();
            types.Add(entityErrorType);
        }
        
        // represent Friflo.Json.Fliox.Hub.Protocol.Models.EntityError
        private static GqlType EntityErrorType() {
            var fields = new List<GqlField> {
                Gql.Field("id",         Gql.Type(Gql.String(), true)),
                Gql.Field("type",       Gql.Type(Gql.String(), true)),
                Gql.Field("message",    Gql.Type(Gql.String(), true))
            };
            return Gql.Object("EntityError", fields);
        }
    }
    
    internal enum Kind {
        Output  = 1,
        Input   = 2
    }
}