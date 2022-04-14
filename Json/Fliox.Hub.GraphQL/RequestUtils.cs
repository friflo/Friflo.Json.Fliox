// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class RequestUtils
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
                error = null;
                return new List<JsonKey>();
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
        
        internal static List<JsonValue> TryGetAnyList(GraphQLValue value, string docStr, out string error) {
            var gqlList = value as GraphQLListValue;
            if (gqlList == null) {
                error = "expect list";
                return null;
            }
            var values = gqlList.Values;
            if (values == null) {
                error = null;
                return new List<JsonValue>();
            }
            var sb      = new StringBuilder();
            var result  = new List<JsonValue>(values.Count);
            foreach (var item in values) {
                sb.Clear();
                var astError    = GetAny(item, sb);
                if (astError != null) {
                    var loc         = astError.location;
                    var astValue    = docStr.Substring(loc.Start, loc.End - loc.Start);
                    error           = $"invalid value at position {loc.Start}. kind: {astError.kind}, value: {astValue}";
                    return new List<JsonValue>();
                }
                result.Add(new JsonValue(sb.ToString()));
            }
            error = null;
            return result;
        }
        
        internal static JsonValue TryGetAny(GraphQLValue value, string docStr, out string error) {
            var sb          = new StringBuilder();
            var astError    = GetAny(value, sb);
            if (astError != null) {
                var loc         = astError.location;
                var astValue    = docStr.Substring(loc.Start, loc.End - loc.Start);
                error           = $"invalid value at position {loc.Start}. kind: {astError.kind}, value: {astValue}";
                return new JsonValue();
            }
            error = null;
            return new JsonValue(sb.ToString());
        }
        
        private static Error GetAny(GraphQLValue value, StringBuilder sb) {
            switch (value.Kind) {
                case ASTNodeKind.NullValue:
                    var nullVal = (GraphQLNullValue)value;
                    sb.Append(nullVal.Value.Span);
                    return null;
                case ASTNodeKind.BooleanValue:
                    var boolVal = (GraphQLBooleanValue)value;
                    sb.Append(boolVal.Value.Span);
                    return null;
                case ASTNodeKind.IntValue:
                    var intVal = (GraphQLIntValue)value;
                    sb.Append(intVal.Value.Span);
                    return null;
                case ASTNodeKind.FloatValue:
                    var fltVal = (GraphQLFloatValue)value;
                    sb.Append(fltVal.Value.Span);
                    return null;
                case ASTNodeKind.StringValue:
                    var strVal = (GraphQLStringValue)value;
                    sb.Append('"');
                    sb.Append(strVal.Value.Span);
                    sb.Append('"');
                    return null;
                case ASTNodeKind.ObjectValue:
                    var obj = (GraphQLObjectValue)value;
                    sb.Append('{');
                    var firstField  = true;
                    var fields      = obj.Fields;
                    if (fields == null) {
                        sb.Append('}');
                        return null;
                    }
                    foreach (var field in fields) {
                        if (firstField) {
                            firstField = false;
                        } else {
                            sb.Append(',');
                        }
                        sb.Append('"');
                        sb.Append(field.Name.StringValue);
                        sb.Append('"');
                        sb.Append(':');
                        var error = GetAny(field.Value, sb);
                        if (error != null)
                            return error;
                    }
                    sb.Append('}');
                    return null;
                case ASTNodeKind.ListValue:
                    var list = (GraphQLListValue)value;
                    sb.Append('[');
                    var firstItem   = true;
                    var values      = list.Values;
                    if (values == null) {
                        sb.Append(']');
                        return null;
                    }
                    foreach (var item in values) {
                        if (firstItem) {
                            firstItem = false;
                        } else {
                            sb.Append(',');
                        }
                        var error = GetAny(item, sb);
                        if (error != null)
                            return error;
                    }
                    sb.Append(']');
                    return null;
            }
            return new Error(value.Kind, value.Location);
        }
        
        private class Error {
            internal    readonly    ASTNodeKind     kind;
            internal    readonly    GraphQLLocation location;
            
            internal Error(ASTNodeKind kind, GraphQLLocation location) {
                this.kind       = kind;
                this.location   = location;
            }
        }
    }
}

#endif
