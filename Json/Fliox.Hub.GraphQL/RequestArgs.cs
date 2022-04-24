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
            var arguments   = query.Arguments;
            if (arguments == null) {
                error = null;
                return null;
            }
            string result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetStringArg (argument.Value, out error);
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return result;
        }
        
        internal static int? GetInt(GraphQLField query, string name, out QueryError? error)
        {
            var arguments   = query.Arguments;
            if (arguments == null) {
                error = null;
                return null;
            }
            int? result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if  (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetIntArg(argument.Value, out error);
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return result;
        }
        
        internal static bool? GetBoolean(GraphQLField query, string name, out QueryError? error)
        {
            var arguments   = query.Arguments;
            if (arguments == null) {
                error = null;
                return null;
            }
            bool? result = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if  (argName.SequenceEqual(name)) {
                    result  = RequestUtils.TryGetBooleanArg(argument.Value, out error);
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return result;
        }
        
        internal static HashSet<JsonKey> GetIds(GraphQLField query, out QueryError? error) {
            var arguments = query.Arguments;
            if (arguments == null) {
                error = null;
                return null;
            }
            List<JsonKey> idList = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("ids")) {
                    idList  = RequestUtils.TryGetIdList (argument, out error);
                }
            }
            if (idList == null) {
                error = new QueryError("missing parameter: ids");
                return null;
            }
            var ids     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                ids.Add(id);
            }
            error = null;
            return ids;
        }
        
        internal static List<JsonValue> GetEntities(GraphQLField query, string docStr, out QueryError? error)
        {
            List<JsonValue> entities = null;
            var arguments = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var argName = argument.Name.Value.Span;
                    if (argName.SequenceEqual("entities")) {
                        entities    = RequestUtils.TryGetAnyList(argument.Value, docStr, out error);
                    } else {
                        error       = new QueryError(RequestUtils.UnknownArgument(argName));
                    }
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return entities;
        }
        
        internal static JsonValue GetParam(GraphQLField query, string docStr, in QueryResolver resolver, out QueryError? error) {
            var args = query.Arguments;
            if (args == null) {
                if (!resolver.hasParam) {
                    error = null;
                    return new JsonValue();
                }
                if (resolver.paramRequired) {
                    error = new QueryError("Expect argument: param");
                } else {
                    error = null;
                }
                return new JsonValue();
            }
            JsonValue result;
            foreach (var argument in args) {
                var argName = argument.Name.Value.Span;
                if (argName.SequenceEqual("param")) {
                    result  = RequestUtils.TryGetAny(argument.Value, docStr, out error);
                } else {
                    error   = new QueryError(RequestUtils.UnknownArgument(argName));
                }
                if (error != null)
                    return new JsonValue();
            }
            error = null;
            return result;
        }
    }
}

#endif
