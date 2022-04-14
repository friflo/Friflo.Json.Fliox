// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class RequestArgs
    {
        internal static HashSet<JsonKey> GetIds(GraphQLField query, out string error) {
            var arguments = query.Arguments;
            if (arguments == null) {
                error = null;
                return null;
            }
            List<JsonKey> idList = null;
            foreach (var argument in arguments) {
                var argName = argument.Name.StringValue;
                switch (argName) {
                    case "ids":     idList  = RequestUtils.TryGetIdList (argument, out error);  break;
                    default:        error   = RequestUtils.UnknownArgument(argName);            break;
                }
                if (error != null)
                    return null;
            }
            if (idList == null) {
                error = "missing parameter: ids";
                return null;
            }
            var ids     = new HashSet<JsonKey>(idList.Count, JsonKey.Equality);
            foreach (var id in idList) {
                ids.Add(id);
            }
            error = null;
            return ids;
        }
        
        internal static List<JsonValue> GetEntities(GraphQLField query, string docStr, out string error)
        {
            List<JsonValue> entities = null;
            var arguments = query.Arguments;
            if (arguments != null) {
                foreach (var argument in arguments) {
                    var argName = argument.Name.StringValue;
                    switch (argName) {
                        case "entities":    entities    = RequestUtils.TryGetAnyList(argument.Value, docStr, out error);    break;
                        default:            error       = RequestUtils.UnknownArgument(argName);                            break;
                    }
                    if (error != null)
                        return null;
                }
            }
            error = null;
            return entities;
        }
        
        internal static JsonValue GetParam(GraphQLArguments args, string docStr, in QueryResolver resolver, out string error) {
            if (args == null) {
                if (!resolver.hasParam) {
                    error = null;
                    return new JsonValue();
                }
                if (resolver.paramRequired) {
                    error = "Expect argument: param";
                } else {
                    error = null;
                }
                return new JsonValue();
            }
            JsonValue result;
            foreach (var argument in args) {
                var argName = argument.Name.StringValue;
                switch (argName) {
                    case "param":   result  = RequestUtils.TryGetAny(argument.Value, docStr, out error);    break;
                    default:        error   = RequestUtils.UnknownArgument(argName);                        break;
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
