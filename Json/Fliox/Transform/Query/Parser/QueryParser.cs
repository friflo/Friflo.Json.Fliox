// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryParser
    {
        private readonly QueryLexer lexer = new QueryLexer();
        
        public Operation Parse (string operation) {
            lexer.Tokenize (operation, out string error);
            return null;
        }
    }
}