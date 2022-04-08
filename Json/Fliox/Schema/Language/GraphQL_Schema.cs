// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class GraphQLGenerator
    {
        private static string CreateSchema(GqlSchema schema) {
            var sb = new StringBuilder();
            foreach (var type in schema.types) {
                switch (type) {
                    case GqlScalar scalarType:  EmitScalar  (scalarType,    sb);    break;
                    case GqlObject obj:         EmitObject  (obj,           sb);    break;
                    case GqlEnum   enumType:    EmitEnum    (enumType,      sb);    break;
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        
        private static void EmitScalar(GqlScalar type, StringBuilder sb) {
            sb.AppendLine($"scalar {type.name}");
        }
        
        private static void EmitObject(GqlObject type, StringBuilder sb) {
            int maxFieldName    = type.fields.MaxLength(field => field.name.Length);
            sb.AppendLine($"type {type.name} {{");
            foreach (var field in type.fields) {
                var fieldType   = GetType(field.type);
                var indent      = Indent(maxFieldName, field.name);
                sb.AppendLine($"    {field.name}: {indent}{fieldType}");
            }
            sb.AppendLine("}");
        }
        
        private static void EmitEnum(GqlEnum type, StringBuilder sb) {
            sb.AppendLine($"enum {type.name} {{");
            foreach (var value in type.enumValues) {
                sb.AppendLine($"    {value.name}");
            }
            sb.AppendLine("}");
        }
        
        
        private static string GetType(GqlType type) {
            switch(type) {
                case GqlScalar      _:
                case GqlObject      _:
                case GqlInterface   _:
                case GqlUnion       _:
                case GqlEnum        _:
                case GqlInputObject _:
                    return type.name;
                case GqlList        list:
                    var itemType = GetType(list.ofType);
                    return $"[{itemType}]";
                case GqlNonNull     nonNull:
                    var nonNullType = GetType(nonNull.ofType);
                    return $"{nonNullType}!";
            }
            throw new InvalidOperationException($"unexpected type: {type.GetType().FullName}");
        }
    }
}