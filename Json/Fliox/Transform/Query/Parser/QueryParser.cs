// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryParser
    {
        public Operation Parse (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation, out error);
            
            var op      = GetOperation (result.items, 0, out error);
            return op;
        }
        
        private Operation GetOperation(Token[] tokens, int pos, out string error) {
            var token = tokens[pos];
            error = null;
            switch (token.type) {
                case TokenType.String:  return new StringLiteral(token.str);
                case TokenType.Double:  return new DoubleLiteral(token.dbl);
                case TokenType.Long:    return new LongLiteral(token.lng);
                default:
                    error = "unexpected token";
                    return null;
            }
        }
    }
}