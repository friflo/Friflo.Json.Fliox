// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class RequestArgs
    {
        internal static bool TryGetString(GraphQLField query, string name, out string value, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value = RequestUtils.TryGetStringArg (argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetInt(GraphQLField query, string name, out int? value, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value  = RequestUtils.TryGetIntArg(argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetBool(GraphQLField query, string name, out bool? value, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error)) {
                value = null;
                return true;
            }
            value = null;
            if (FindArgument(arguments, name, out var argument)) {
                value = RequestUtils.TryGetBooleanArg(argument.Value, name, out error);
                if (error != null)
                    return false;
            }
            return true;
        }
        
        internal static bool TryGetIds(GraphQLField query, string name, out HashSet<JsonKey> value, out QueryError? error) {
            if (!GetArguments(query, out var arguments, out error)) {
                value = null;
                return true;
            }
            List<JsonKey> idList = null;
            if (FindArgument(arguments, name, out var argument)) {
                idList  = RequestUtils.TryGetStringList (argument, name, out error);
            }
            if (idList == null) {
                error = new QueryError(name, "missing parameter");
                value = null;
                return false;
            }
            value     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                value.Add(id);
            }
            return true;
        }

        internal static List<JsonValue> GetEntities(GraphQLField query, string docStr, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error))
                return null;
            List<JsonValue> entities = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("entities")) {
                    entities    = RequestUtils.TryGetAnyList(argument.Value, "entities", docStr, out error);
                } else {
                    error       = new QueryError(argument.Name.StringValue, RequestUtils.UnknownArgument);
                }
                if (error != null)
                    return null;
            }
            return entities;
        }
        
        internal static JsonValue GetParam(GraphQLField query, string docStr, in QueryResolver resolver, out QueryError? error) {
            if (!GetArguments(query, out var arguments, out error)) {
                if (!resolver.hasParam) {
                    return new JsonValue();
                }
                if (resolver.paramRequired) {
                    error = new QueryError(null, "Expect argument: param");
                }
                return new JsonValue();
            }
            JsonValue result;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("param")) {
                    result  = RequestUtils.TryGetAny(argument.Value, "param", docStr, out error);
                } else {
                    error   = new QueryError(argument.Name.StringValue, RequestUtils.UnknownArgument);
                }
                if (error != null)
                    return new JsonValue();
            }
            return result;
        }
        
        // ------------------------------------------ utils ------------------------------------------ 
        private static bool GetArguments (GraphQLField query, out GraphQLArguments args, out QueryError? error) {
            error = null;
            args = query.Arguments;
            return args != null;
        }
        
        private static bool FindArgument (GraphQLArguments arguments, string name, out GraphQLArgument argument) {
            foreach (var args in arguments) {
                var argName = args.Name.Value.Span;
                if (!argName.SequenceEqual(name))
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
