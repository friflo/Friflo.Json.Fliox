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
        internal static bool TryGetStringArg(GraphQLValue gqlValue, out string value, out string error) {
            var stringValue = gqlValue as GraphQLStringValue;
            if (stringValue == null) {
                value = null;
                error = "expect string argument";
                return false;
            }
            value = stringValue.Value.ToString();
            error = null;
            return true;
        }
        
        internal static bool TryGetIntArg(GraphQLValue gqlValue, out int? value, out string error) {
            var gqlIntValue = gqlValue as GraphQLIntValue;
            if (gqlIntValue == null) {
                value = null;
                error = "expect string argument";
                return false;
            }
            var stringValue = gqlIntValue.Value.ToString(); // todo avoid string creation
            if (!int.TryParse(stringValue, out var intValue)) {
                value = null;
                error = "invalid integer";
                return false;
            }
            value = intValue;
            error = null;
            return true;
        }
        
        internal static bool TryGetIdList(GraphQLArgument arg, out List<JsonKey> value, out string error) {
            var gqlList = arg.Value as GraphQLListValue;
            if (gqlList == null) {
                value = null;
                error = "expect string array";
                return false;
            }
            var values = gqlList.Values;
            if (values == null) {
                error = "invalid string array";
                value = null;
                return false;
            }
            value = new List<JsonKey>(values.Count);
            foreach (var item in values) {
                if (!TryGetStringArg(item, out string stringValue, out error))
                    return false;
                value.Add(new JsonKey(stringValue));
            }
            error = null;
            return true;
        }
    }
}

#endif
