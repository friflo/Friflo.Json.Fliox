// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class RequestArgs
    {
        /// enforce "o" as lambda argument
        // private const string DefaultParam   = "o";
        
        internal static bool TryGetFilter(in QueryContext cx, string name, out string value, out QueryError? error)
        {
            if (!TryGetString(cx, name, out value, out error))
                return false;
            if (value == null)
                return true;
            // var env   = new QueryEnv(DefaultParam); 
            var op    = Operation.Parse(value, out var filterError);
            if (filterError != null) {
                error = new QueryError(name, filterError);
                return false;
            }
            if (op is FilterOperation _) {
                return true;
            }
            error = new QueryError(name, "expect predicate expression");
            return false;
        }
        
        internal static bool TryGetString(in QueryContext cx, string name, out string value, out QueryError? error)
        {
            if (!GetArguments(cx, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value = RequestUtils.TryGetStringArg (cx, argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetEnumValue(in QueryContext cx, string name, out string value, out QueryError? error)
        {
            if (!GetArguments(cx, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value = RequestUtils.TryGetEnumValueArg(cx, argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetInt(in QueryContext cx, string name, out int? value, out QueryError? error)
        {
            if (!GetArguments(cx, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value  = RequestUtils.TryGetIntArg(cx, argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetBool(in QueryContext cx, string name, out bool? value, out QueryError? error)
        {
            if (!GetArguments(cx, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value = RequestUtils.TryGetBooleanArg(cx, argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetIds(in QueryContext cx, string name, out ListOne<JsonKey> value, out QueryError? error) {
            if (!GetArguments(cx, out var arguments, out error)) {
                value = null;
                return true;
            }
            List<JsonKey> idList = null;
            if (FindArgument(arguments, name, out var argument)) {
                idList  = RequestUtils.TryGetStringList (cx, argument, name, out error);
                if (error != null) {
                    value = null;
                    return false;
                }
            }
            if (idList == null) {
                error = new QueryError(name, "missing parameter");
                value = null;
                return false;
            }
            value     = new ListOne<JsonKey>(idList.Count);
            foreach (var id in idList) {
                value.Add(id);
            }
            return true;
        }

        internal static List<JsonEntity> GetEntities(in QueryContext cx, out QueryError? error)
        {
            if (!GetArguments(cx, out var arguments, out error))
                return null;
            List<JsonEntity> entities = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("entities".AsSpan())) {
                    entities    = RequestUtils.TryGetAnyList(cx, argument.Value, "entities", out error);
                } else {
                    error       = new QueryError(argument.Name.StringValue, RequestUtils.UnknownArgument);
                }
                if (error != null)
                    return null;
            }
            return entities;
        }
        
        internal static JsonValue GetParam(in QueryContext cx, out QueryError? error) {
            if (!GetArguments(cx, out var arguments, out error)) {
                if (!cx.resolver.hasParam) {
                    return new JsonValue();
                }
                if (cx.resolver.paramRequired) {
                    error = new QueryError(null, "Expect argument: param");
                }
                return new JsonValue();
            }
            JsonValue result = default;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("param".AsSpan())) {
                    result  = RequestUtils.TryGetAny(cx, argument.Value, "param", out error);
                } else {
                    error   = new QueryError(argument.Name.StringValue, RequestUtils.UnknownArgument);
                }
                if (error != null)
                    return new JsonValue();
            }
            return result;
        }
        
        // ------------------------------------------ utils ------------------------------------------ 
        private static bool GetArguments (in QueryContext cx, out GraphQLArguments args, out QueryError? error) {
            error = null;
            args = cx.query.Arguments;
            return args != null;
        }
        
        private static bool FindArgument (GraphQLArguments arguments, string name, out GraphQLArgument argument) {
            foreach (var args in arguments) {
                var argName = args.Name.Value.Span;
                if (!argName.SequenceEqual(name.AsSpan()))
                    continue;
                argument = args;
                return true;
            }
            argument = null;
            return false;
        }
    }
}

#endif
