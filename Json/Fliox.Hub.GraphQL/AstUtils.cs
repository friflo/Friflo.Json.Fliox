// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class AstUtils
    {
        internal static string UnknownArgument(string argName) => $"unknown argument: {argName}";

        internal static string TryGetStringArg(GraphQLValue gqlValue, out string error) {
            var stringValue = gqlValue as GraphQLStringValue;
            if (stringValue == null) {
                error = "expect string argument";
                return null;
            }
            error = null;
            return stringValue.Value.ToString();
        }
        
        internal static int? TryGetIntArg(GraphQLValue gqlValue, out string error) {
            var gqlIntValue = gqlValue as GraphQLIntValue;
            if (gqlIntValue == null) {
                error = "expect string argument";
                return null;
            }
            var stringValue = gqlIntValue.Value.ToString(); // todo avoid string creation
            if (!int.TryParse(stringValue, out var intValue)) {
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
    }
}

#endif
