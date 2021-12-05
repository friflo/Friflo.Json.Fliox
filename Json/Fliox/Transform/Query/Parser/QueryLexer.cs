// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryLexer
    {
        private const int End = -1;
        
        public TokenList Tokenize(string operation, out string error) {
            var     tokens      = new List<Token>();
            int     pos         = 0;
            var     lastType    = TokenType.Start;
            while (true) {
                var token = GetToken(lastType, operation, ref pos, out error);
                if (token.type == TokenType.End)
                    return new TokenList(tokens.ToArray());
                lastType = token.type;
                tokens.Add(token);
            }
        }
        
        private static Token GetToken(TokenType lastType, string operation, ref int pos, out string error) {
            int c = NextChar(operation, pos);
            error = null;
            switch (c) {
                case '+':   pos++;
                    if (!IsOperand(lastType)) {
                        c = NextChar(operation, pos);
                        if ('0' <= c && c <= '9')
                            return GetNumber(false, operation, ref pos, out error);
                    }
                    return new Token(TokenType.Add);
                case '-':   pos++;
                    if (!IsOperand(lastType)) {
                        c = NextChar(operation, pos);
                        if ('0' <= c && c <= '9')
                            return GetNumber(true, operation, ref pos, out error);
                    }
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
                case '\'':   pos++;
                    return GetString(operation, ref pos, out error);
                case End:
                    return new Token(TokenType.End);
                default:
                    return GetSymbol(c, operation, ref pos, out error);
            }
        }

        private static int NextChar(string operation, int pos) {
            if (pos < operation.Length) {
                return operation[pos];
            }
            return End;
        }
        
        private static bool IsOperand(TokenType type) {
            return type == TokenType.Symbol || type == TokenType.Long || type == TokenType.Double || type == TokenType.BracketClose;
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
            int     start = pos++;
            bool    isFloat = false;
            while (true) {
                int c = NextChar(operation, pos);
                if ('0' <= c && c <= '9') {
                    pos++;
                    continue;
                }
                if ('.' == c) {
                    if (isFloat) {
                        error = "invalid floating point number";
                        return new Token(TokenType.Error);
                    }
                    pos++;
                    isFloat = true;
                    continue;
                }
                break;
            }
            error = null;
            var str = operation.Substring(start, pos - start);
            if (isFloat) {
                double dbl = double.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                dbl = negative ? -dbl : dbl;
                return new Token(dbl);
            }
            long lng = long.Parse(str);
            lng = negative ? -lng : lng;
            return new Token(lng);
        }
        
        private static Token GetString(string operation, ref int pos, out string error) {
            int start = pos;
            while (true) {
                int c = NextChar(operation, pos);
                if (c == End) {
                    error = "missing string terminator '\"'";
                    return new Token(TokenType.Error);
                }
                if (c == '\'') {
                    error = null;
                    var str = operation.Substring(start, pos - start);
                    pos++;
                    return new Token(TokenType.String, str);
                }
                pos++;
            }
        }
    }
}