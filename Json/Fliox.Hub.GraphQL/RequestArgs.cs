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
        internal static string GetString(GraphQLField query, string name, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error))
                return null;
            string result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetStringArg (argument.Value, name, out error);
                    if (error != null)
                        return null;
                }
            }
            return result;
        }
        
        internal static int? GetInt(GraphQLField query, string name, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error))
                return null;
            int? result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if  (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetIntArg(argument.Value, name, out error);
                    if (error != null)
                        return null;
                }
            }
            return result;
        }
        
        internal static bool? GetBoolean(GraphQLField query, string name, out QueryError? error)
        {
            if (!GetArguments(query, out var arguments, out error))
                return null;
            bool? result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if  (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetBooleanArg(argument.Value, name, out error);
                    if (error != null)
                        return null;
                }
            }
            return result;
        }
        
        internal static HashSet<JsonKey> GetIds(GraphQLField query, out QueryError? error) {
            if (!GetArguments(query, out var arguments, out error))
                return null;
            List<JsonKey> idList = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("ids")) {
                    idList  = RequestUtils.TryGetStringList (argument, "ids", out error);
                }
            }
            if (idList == null) {
                error = new QueryError(null, "missing parameter: ids");
                return null;
            }
            var ids     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                ids.Add(id);
            }
            return ids;
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
                    error       = new QueryError(null, RequestUtils.UnknownArgument(argName));
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
                    error   = new QueryError(null, RequestUtils.UnknownArgument(argName));
                }
                if (error != null)
                    return new JsonValue();
            }
            return result;
        }
        
        private static bool GetArguments (GraphQLField query, out GraphQLArguments args, out QueryError? error) {
            error = null;
            args = query.Arguments;
            return args != null;
        }
    }
}

#endif
