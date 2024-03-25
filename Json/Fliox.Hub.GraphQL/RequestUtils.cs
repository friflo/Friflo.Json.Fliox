// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GraphQLParser.AST;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class RequestUtils
    {
        internal static string UnknownArgument = "unknown argument";

        internal static string TryGetStringArg(in QueryContext cx, GraphQLValue gqlValue, string name, out QueryError? error) {
            if (gqlValue is GraphQLStringValue strVal) {
                error = null;
                return strVal.Value.ToString();
            }
            if (gqlValue is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<string>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect string", gqlValue, cx.doc);
            return null;
        }
        
        internal static string TryGetEnumValueArg(in QueryContext cx, GraphQLValue gqlValue, string name, out QueryError? error) {
            if (gqlValue is GraphQLEnumValue strVal) {
                error = null;
                return strVal.Name.StringValue;
            }
            if (gqlValue is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<string>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect string", gqlValue, cx.doc);
            return null;
        }
        
        internal static int? TryGetIntArg(in QueryContext cx, GraphQLValue gqlValue, string name, out QueryError? error) {
            if (gqlValue is GraphQLIntValue gqlIntValue) {
                var strVal = gqlIntValue.Value.Span;
                if (!MathExt.TryParseInt(strVal, NumberStyles.None, CultureInfo.InvariantCulture, out var intValue)) {
                    error = QueryError(name, "invalid int", gqlValue, cx.doc);
                    return null;
                }
                error = null;
                return intValue;
            }
            if (gqlValue is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<int?>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect int", gqlValue, cx.doc);
            return null;
        }
        
        internal static bool? TryGetBooleanArg(in QueryContext cx, GraphQLValue gqlValue, string name, out QueryError? error) {
            if (gqlValue is GraphQLBooleanValue gqlBooleanValue) {
                error = null;
                return gqlBooleanValue.BoolValue;
            }
            if (gqlValue is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<bool?>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect boolean", gqlValue, cx.doc);
            return null;
        }
        
        internal static List<JsonKey> TryGetStringList(in QueryContext cx, GraphQLArgument arg, string name, out QueryError? error) {
            var value = arg.Value;
            if (value is GraphQLListValue gqlList) {
                var values = gqlList.Values;
                if (values == null) {
                    error = null;
                    return new List<JsonKey>();
                }
                var result = new List<JsonKey>(values.Count);
                foreach (var item in values) {
                    var stringValue = TryGetStringArg(cx, item, name, out error);
                    if (error != null)
                        return null;
                    result.Add(new JsonKey(stringValue));
                }
                error = null;
                return result;
            }
            if (value is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<List<JsonKey>>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect string array", arg.Value, cx.doc);
            return null;
        }
        
        internal static List<JsonEntity> TryGetAnyList(in QueryContext cx, GraphQLValue value, string name, out QueryError? error) {
            if (value is GraphQLListValue gqlList) {
                var values = gqlList.Values;
                if (values == null) {
                    error = null;
                    return new List<JsonEntity>();
                }
                var sb      = new StringBuilder();
                var result  = new List<JsonEntity>(values.Count);
                foreach (var item in values) {
                    sb.Clear();
                    var astError    = GetAny(item, sb);
                    if (astError != null) {
                        var loc         = astError.location;
                        var astValue    = cx.doc.Substring(loc.Start, loc.End - loc.Start);
                        error           = new QueryError(name, $"invalid value at position {loc.Start}. kind: {astError.kind}, value: {astValue}");
                        return null;
                    }
                    result.Add(new JsonEntity(new JsonValue(sb.ToString())));
                }
                error = null;
                return result;
            }
            if (value is GraphQLVariable gqlVariable) {
                return cx.ReadVariable<List<JsonEntity>>(cx, gqlVariable, name, out error);
            }
            error = QueryError(name, "expect list", value, cx.doc);
            return null;
        }
        
        internal static JsonValue TryGetAny(in QueryContext cx, GraphQLValue value, string name, out QueryError? error) {
            var sb          = new StringBuilder();
            var astError    = GetAny(value, sb);
            if (astError != null) {
                var loc         = astError.location;
                var astValue    = cx.doc.Substring(loc.Start, loc.End - loc.Start);
                error           = new QueryError(name, $"invalid value at position {loc.Start}. kind: {astError.kind}, value: {astValue}");
                return new JsonValue();
            }
            error = null;
            return new JsonValue(sb.ToString());
        }
        
        internal static QueryError QueryError(string name, string message, GraphQLValue was, string doc) {
            var sb = new StringBuilder();
            sb.Append(message);
            sb.Append(". was: ");
            var loc = was.Location;
            sb.Append(doc, loc.Start, loc.End - loc.Start);
            return new QueryError(name, sb.ToString());
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
                    AppendEscString(sb, strVal.Value.Span);
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
                        sb.Append(field.Name.Value.Span);
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
        
        private static void AppendEscString(StringBuilder sb, in ReadOnlySpan<char> value) {
            var len = value.Length;
            for (int n = 0; n < len; n++) {
                var c = value[n];
                if (c == '"' || c == '\\') {
                    sb.Append('\\');
                    sb.Append(c);
                } else {
                    sb.Append(c);
                }
            }
        }
        
        private sealed class Error {
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
