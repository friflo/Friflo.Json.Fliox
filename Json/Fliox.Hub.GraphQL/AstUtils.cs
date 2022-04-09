// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using GraphQLParser.AST;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class AstUtils
    {
        internal static bool TryGetStringArg(GraphQLArgument arg, out string value, out string error) {
            var stringValue = arg.Value as GraphQLStringValue;
            if (stringValue == null) {
                value = null;
                error = "expect string argument";
                return false;
            }
            value = stringValue.Value.ToString();
            error = null;
            return true;
        }
        
        internal static bool TryGetIntArg(GraphQLArgument arg, out int? value, out string error) {
            var gqlIntValue = arg.Value as GraphQLIntValue;
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
    }
}

#endif
