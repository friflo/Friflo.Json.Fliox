// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryParser
    {
        public Operation Parse (string operation, out string error) {
            var result      = QueryLexer.Tokenize (operation, out error);
            var tokens      = result.items;
            var pos         = 0;
            Operation op    = null;
            while (pos < tokens.Length) {
                op      = GetOperation (tokens, ref pos, op, out error);
            }
            return op;
        }
        
        private Operation GetOperation(Token[] tokens, ref int pos, Operation op, out string error) {
            var token = tokens[pos];
            Operation right;
            error = null;
            
            switch (token.type)
            {
            // --- binary tokens
            case TokenType.Add:             right = GetRight(tokens, ref pos, op, out error); return new Add        (op, right);
            case TokenType.Sub:             right = GetRight(tokens, ref pos, op, out error); return new Subtract   (op, right);
            case TokenType.Mul:             right = GetRight(tokens, ref pos, op, out error); return new Multiply   (op, right);
            case TokenType.Div:             right = GetRight(tokens, ref pos, op, out error); return new Divide     (op, right);
            //
            case TokenType.Greater:         right = GetRight(tokens, ref pos, op, out error); return new GreaterThan        (op, right);
            case TokenType.GreaterOrEqual:  right = GetRight(tokens, ref pos, op, out error); return new GreaterThanOrEqual (op, right);
            case TokenType.Less:            right = GetRight(tokens, ref pos, op, out error); return new LessThan           (op, right);
            case TokenType.LessOrEqual:     right = GetRight(tokens, ref pos, op, out error); return new LessThanOrEqual    (op, right);
            case TokenType.Equals:          right = GetRight(tokens, ref pos, op, out error); return new Equal              (op, right);
            case TokenType.NotEquals:       right = GetRight(tokens, ref pos, op, out error); return new NotEqual           (op, right);
            
            // --- unary tokens
            case TokenType.String:  pos++; return new StringLiteral(token.str);
            case TokenType.Double:  pos++; return new DoubleLiteral(token.dbl);
            case TokenType.Long:    pos++; return new LongLiteral(token.lng);
            
            case TokenType.Symbol:
                pos++;
                if (token.str == "true")    return new TrueLiteral();
                if (token.str == "false")   return new FalseLiteral();
                if (token.str == "null")    return new NullLiteral();
                error = $"unexpected symbol: {token.str}";
                return null;
            
            // --- group tokens
            /*case TokenType.BracketOpen:
                pos++;
                if (op is Field) {}
                var first = GetOperation(tokens, ref pos, null, out error);
                
                break; */
            default:
                error = "unexpected token";
                return null;
            }
        }
        
        private Operation GetRight(Token[] tokens, ref int pos, Operation left, out string error) {
            if (left == null) return Error("missing left operand for +", out error);
            pos++;
            return GetOperation(tokens, ref pos, null, out error);
        }

        private Operation Error (string message, out string error) {
            error = message;
            return null;
        }
    }
}