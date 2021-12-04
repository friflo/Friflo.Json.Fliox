// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryLexer
    {
        private const int End = -1;
        
        public TokenList Tokenize(string operation, out string error) {
            var     tokens  = new List<Token>();
            int     pos     = 0;
            while (true) {
                var token = GetToken(operation, ref pos, out error);
                if (token.type == TokenType.End)
                    return new TokenList(tokens.ToArray());
                tokens.Add(token);
            }
        }
        
        private static Token GetToken(string operation, ref int pos, out string error) {
            int c = NextChar(operation, pos);
            error = null;
            switch (c) {
                case '+':   pos++;
                    c = NextChar(operation, pos);
                    if ('0' <= c && c <= '9')
                        return GetNumber(false, operation, ref pos, out error);
                    return new Token(TokenType.Add);
                case '-':   pos++;
                    c = NextChar(operation, pos);
                    if ('0' <= c && c <= '9')
                        return GetNumber(true, operation, ref pos, out error);
                    return new Token(TokenType.Sub);
                case '*':   pos++; return new Token(TokenType.Mul);
                case '/':   pos++; return new Token(TokenType.Div);
                case '.':   pos++; return new Token(TokenType.Dot);
                case '(':   pos++; return new Token(TokenType.BracketOpen);
                case ')':   pos++; return new Token(TokenType.BracketClose);
                case '>':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.GreaterOrEqual);
                    }
                    return new Token(TokenType.Greater);
                case '<':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.LowerOrEqual);
                    }
                    return new Token(TokenType.Lower);
                case '!':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.NotEquals);
                    }
                    return new Token(TokenType.Not);
                case '=':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.Equals);
                    }
                    if (c == '>') {
                            pos++; return new Token(TokenType.Arrow);
                    }
                    error = $"unexpected character: '${(char)c}'";
                    return new Token(TokenType.Error);
                case '|':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '|') {
                            pos++; return new Token(TokenType.Or);
                    }
                    error = $"expect character '|'. was: '${(char)c}'";
                    return new Token(TokenType.Error);
                case '&':   pos++;
                    c = NextChar(operation, pos);
                    if (c == '&') {
                            pos++; return new Token(TokenType.And);
                    }
                    error = $"expect character '&'. was: '${(char)c}'";
                    return new Token(TokenType.Error);
                case -1:
                    return new Token(TokenType.End);
                default:
                    return GetSymbol(c, operation, ref pos, out error);
            }
        }

        private static int NextChar(string operation, int pos) {
            if (pos < operation.Length) {
                return operation[pos];
            }
            return -1;
        }

        private static Token GetSymbol(int c, string operation, ref int pos, out string error) {
            if ('0' <= c && c <= '9') {
                return GetNumber (false, operation, ref pos, out error);
            }
            if (IsChar(c)) {
                var start = pos;
                while (true) {
                    pos++;
                    c = NextChar(operation, pos);
                    if (IsChar(c))
                        continue;
                    var str = operation.Substring(start, pos - start);
                    error = null;
                    return new Token(TokenType.Symbol, str);
                }
            }
            error = $"unexpected character: '${(char)c}'";
            return default;
        }
        
        private static bool IsChar(int c) {
            return  'a' <= c && c <= 'z' ||
                    'A' <= c && c <= 'Z' ||
                    '_' == c;
        }

        private static Token GetNumber(bool negative, string operation, ref int pos, out string error) {
            error = null;
            int start = pos++;
            int c;
            do {
                c = NextChar(operation, pos);
            }
            while ('0' <= c && c <= '9');
            var str = operation.Substring(start, pos - start);
            long lng = long.Parse(str);
            lng = negative ? -lng : lng;
            return new Token(lng);
        }
    }
}