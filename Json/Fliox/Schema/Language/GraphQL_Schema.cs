// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Schema.Utils;
using static Friflo.Json.Fliox.Schema.Language.Generator;

namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed partial class GraphQLGenerator
    {
        private static string CreateSchema(GqlSchema schema, SchemaInfo schemaInfo) {
            var sb = new StringBuilder();
            EmitMetaInfo(schemaInfo, sb);
            foreach (var type in schema.types) {
                switch (type) {
                    case GqlScalar      scalarType:  EmitScalar     (scalarType,    sb);    break;
                    case GqlObject      obj:         EmitObject     (obj,           sb);    break;
                    case GqlUnion       unionType:   EmitUnion      (unionType,     sb);    break;
                    case GqlEnum        enumType:    EmitEnum       (enumType,      sb);    break;
                    case GqlInputObject inputObject: EmitInputObject(inputObject,   sb);    break;
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        
        private static void EmitMetaInfo(SchemaInfo si, StringBuilder sb) {
            sb.AppendLine($"# {Note}");
            sb.AppendLine();
            if (si == null)
                return;
            Comment("version",          si.version,         sb);
            Comment("termsOfService",   si.termsOfService,  sb);
            Comment("contactName",      si.contactName,     sb);
            Comment("contactUrl",       si.contactUrl,      sb);
            Comment("contactEmail",     si.contactEmail,    sb);
            sb.AppendLine();
        }

        private static void Comment(string key, string value, StringBuilder sb) {
            if (string.IsNullOrEmpty(value))
                return;
            sb.Append("# ");
            sb.Append(key);
            sb.Append(": ");
            sb.Append(Indent(14, key));
            sb.AppendLine(value);
        }

        private static void EmitScalar(GqlScalar type, StringBuilder sb) {
            sb.AppendLine($"scalar {type.name}");
        }
        
        private static void EmitObject(GqlObject type, StringBuilder sb) {
            int maxFieldName    = type.fields.MaxLength(field => field.name == "__all" ? 0 : field.name.Length);
            sb.AppendLine($"type {type.name} {{");
            foreach (var field in type.fields) {
                if (field.name == "__all")
                    continue;
                var fieldType   = GetType(field.type);
                var indent      = Indent(maxFieldName, field.name);
                var args        = GetArgs(field.args);
                sb.AppendLine($"    {field.name}{indent}{args} : {fieldType}");
            }
            sb.AppendLine("}");
        }
        
        private static string GetArgs(List<GqlInputValue> args) {
            if (args.Count == 0)
                return "";
            var sb = new StringBuilder();
            sb.Append("(");
            bool firstArg = true;
            foreach (var arg in args) {
                if (firstArg) {
                    firstArg = false;
                } else {
                    sb.Append(", ");
                }
                sb.Append(arg.name);
                sb.Append(": ");
                var type = GetType(arg.type);
                sb.Append(type);
            }
            sb.Append(")");
            return sb.ToString();
       }
        
        private static void EmitInputObject(GqlInputObject type, StringBuilder sb) {
            int maxFieldName    = type.inputFields.MaxLength(field => field.name.Length);
            sb.AppendLine($"input {type.name} {{");
            foreach (var field in type.inputFields) {
                var fieldType   = GetType(field.type);
                var indent      = Indent(maxFieldName, field.name);
                sb.AppendLine($"    {field.name}{indent} : {fieldType}");
            }
            sb.AppendLine("}");
        }
        
        private static void EmitUnion(GqlUnion type, StringBuilder sb) {
            sb.AppendLine($"union {type.name} =");
            foreach (var itemType in type.possibleTypes) {
                sb.AppendLine($"    | {itemType.name}");
            }
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
                case null:
                    return "Any"; // for messages - as they have no return value
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