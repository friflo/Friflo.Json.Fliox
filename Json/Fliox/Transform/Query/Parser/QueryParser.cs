// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryParser
    {
        public Operation Parse (string operation) {
            QueryLexer.Tokenize (operation, out string error);
            return null;
        }
    }
}