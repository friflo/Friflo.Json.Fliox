// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Parser;

namespace Friflo.Json.Fliox.Transform
{
    public static class QueryParser
    {
        public static Operation Parse (string operation, out string error) {
            return QueryBuilder.Parse(operation, out error);
        }
    }
}