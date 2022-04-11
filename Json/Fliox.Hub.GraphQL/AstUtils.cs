// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class AstUtils
    {
        internal static string UnknownArgument(string argName) => $"unknown argument: {argName}";

        internal static string TryGetStringArg(GraphQLValue gqlValue, out string error) {
            var strVal = gqlValue as GraphQLStringValue;
            if (strVal == null) {
                error = "expect string argument";
                return null;
            }
            error = null;
            return strVal.Value.ToString();
        }
        
        internal static int? TryGetIntArg(GraphQLValue gqlValue, out string error) {
            var gqlIntValue = gqlValue as GraphQLIntValue;
            if (gqlIntValue == null) {
                error = "expect int argument";
                return null;
            }
            var strVal = gqlIntValue.Value.Span;
            if (!int.TryParse(strVal, out var intValue)) {
                error = "invalid integer";
                return null;
            }
            error = null;
            return intValue;
        }
        
        internal static List<JsonKey> TryGetIdList(GraphQLArgument arg, out string error) {
            var gqlList = arg.Value as GraphQLListValue;
            if (gqlList == null) {
                error = "expect string array";
                return null;
            }
            var values = gqlList.Values;
            if (values == null) {
                error = "invalid string array";
                return null;
            }
            var result = new List<JsonKey>(values.Count);
            foreach (var item in values) {
                var stringValue = TryGetStringArg(item, out error);
                if (error != null)
                    return null;
                result.Add(new JsonKey(stringValue));
            }
            error = null;
            return result;
        }
        
        internal static JsonValue TryGetAny(GraphQLValue value, out string error) {
            var sb = new StringBuilder();
            GetAny(value, sb);
            error = null;
            return new JsonValue(sb.ToString());
        }
        
        private static void GetAny(GraphQLValue value, StringBuilder sb) {
            switch (value.Kind) {
                case ASTNodeKind.NullValue:
                    sb.Append("null");
                    break;
                case ASTNodeKind.IntValue:
                    var intVal = (GraphQLIntValue)value;
                    sb.Append(intVal.Value.Span);
                    break;
                case ASTNodeKind.FloatValue:
                    var fltVal = (GraphQLFloatValue)value;
                    sb.Append(fltVal.Value.Span);
                    break;
                case ASTNodeKind.StringValue:
                    var strVal = (GraphQLStringValue)value;
                    sb.Append('"');
                    sb.Append(strVal.Value.Span);
                    sb.Append('"');
                    break;
                case ASTNodeKind.ObjectValue:
                    var obj = (GraphQLObjectValue)value;
                    sb.Append('{');
                    var firstField = true;
                    if (obj.Fields != null) {
                        foreach (var field in obj.Fields) {
                            if (firstField) {
                                firstField = false;
                            } else {
                                sb.Append(", ");
                            }
                            sb.Append('"');
                            sb.Append(field.Name.StringValue);
                            sb.Append("\": ");
                            GetAny(field.Value, sb);
                        }
                    }
                    sb.Append('}');
                    break;
                case ASTNodeKind.ListValue:
                    var list = (GraphQLListValue)value;
                    sb.Append('[');
                    var firstItem = true;
                    if (list.Values != null) {
                        foreach (var item in list.Values) {
                            if (firstItem) {
                                firstItem = false;
                            } else {
                                sb.Append(", ");
                            }
                            GetAny(item, sb);
                        }
                    }
                    sb.Append(']');
                    break;
                default:
                    throw new InvalidOperationException($"unexpected Kind: {value.Kind}");
            }
        }
    }
}

#endif
