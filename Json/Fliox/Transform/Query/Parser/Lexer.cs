// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    /// <summary>
    /// <see cref="QueryLexer"/> iterate the characters of the given operation and create a list
    /// of <see cref="Token"/>s.
    /// </summary>
    public static class QueryLexer
    {
        private const int End = -1;
        
        public static TokenList Tokenize(string operation, out string error) {
            var     tokens      = new List<Token>();
            int     pos         = 0;
            var     lastType    = TokenType.Start;
            while (true) {
                var token = GetToken(lastType, operation, ref pos, out error);
                switch (token.type) {
                    case TokenType.End:
                        return new TokenList(tokens.ToArray());
                    case TokenType.Whitespace:
                        break;
                    case TokenType.Error:
                        return default;
                    default:
                        lastType = token.type;
                        tokens.Add(token);
                        break;
                }
            }
        }
        
        private static Token GetToken(TokenType lastType, string operation, ref int pos, out string error) {
            int c = GetChar(operation, pos++);
            error = null;
            switch (c) {
                case '+':
                    if (!IsOperand(lastType)) {
                        c = GetChar(operation, pos);
                        if (IsDigit(c))
                            return GetNumber(false, operation, ref pos, out error);
                    }
                    return new Token(TokenType.Add, pos);
                case '-':
                    if (!IsOperand(lastType)) {
                        c = GetChar(operation, pos);
                        if (IsDigit(c))
                            return GetNumber(true, operation, ref pos, out error);
                    }
                    return new Token(TokenType.Sub, pos);
                case '*':   return new Token(TokenType.Mul, pos);
                case '/':   return new Token(TokenType.Div, pos);
                case '%':   return new Token(TokenType.Mod, pos);
                case '(':   return new Token(TokenType.BracketOpen, pos);
                case ')':   return new Token(TokenType.BracketClose, pos);
                case '>':   
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.GreaterOrEqual, pos - 1);
                    }
                    return new Token(TokenType.Greater, pos);
                case '<':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.LessOrEqual, pos - 1);
                    }
                    return new Token(TokenType.Less, pos);
                case '!':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.NotEquals, pos - 1);
                    }
                    return new Token(TokenType.Not, pos);
                case '=':
                    c = GetChar(operation, pos);
                    if (c == '=') {
                            pos++; return new Token(TokenType.Equals, pos - 1);
                    }
                    if (c == '>') {
                            pos++; return new Token(TokenType.Arrow, pos - 1);
                    }
                    error = $"invalid operator '='. Use == or => {At} {pos}";
                    return new Token(TokenType.Error, pos);
                case '|':
                    c = GetChar(operation, pos);
                    if (c == '|') {
                            pos++; return new Token(TokenType.Or, pos - 1);
                    }
                    error = $"unexpected character '{(char)c}' after '|'. Use || {At} {pos}";
                    return new Token(TokenType.Error, pos);
                case '&':
                    c = GetChar(operation, pos);
                    if (c == '&') {
                            pos++; return new Token(TokenType.And, pos - 1);
                    }
                    error = $"unexpected character '{(char)c}' after '&'. Use && {At} {pos}";
                    return new Token(TokenType.Error, pos);
                case '"':
                case '\'':
                    return GetString(operation, c, ref pos, out error);
                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    SkipWhitespace(operation, ref pos);
                    return new Token(TokenType.Whitespace, pos);
                case End:
                    pos--;
                    return new Token(TokenType.End, pos + 1);
                default:
                    pos--;
                    return GetSymbol(c, operation, ref pos, out error);
            }
        }
        
        private const string At = "at pos";
        
        private static int GetChar(string operation, int pos) {
            if (pos < operation.Length) {
                return operation[pos];
            }
            return End;
        }
        
        private static bool IsFirstSymbolChar(int c) {
            return  'a' <= c && c <= 'z' ||
                    'A' <= c && c <= 'Z' ||
                    '_' == c || '.' == c;
        }
        
        private static bool IsSymbolChar(int c) {
            return  '0' <= c && c <= '9' ||
                    'a' <= c && c <= 'z' ||
                    'A' <= c && c <= 'Z' ||
                    '_' == c || '.' == c;
        }
        
        private static bool IsDigit(int c) {
            return  '0' <= c && c <= '9';
        }
        
        private static bool IsOperand(TokenType type) {
            return type == TokenType.Symbol || type == TokenType.Long || type == TokenType.Double || type == TokenType.BracketClose;
        }

        private static Token GetSymbol(int c, string operation, ref int pos, out string error) {
            if (IsDigit(c)) {
                return GetNumber (false, operation, ref pos, out error);
            }
            if (IsFirstSymbolChar(c)) {
                var start = pos;
                while (true) {
                    pos++;
                    c = GetChar(operation, pos);
                    if (IsSymbolChar(c))
                        continue;
                    var str = operation.Substring(start, pos - start);
                    SkipWhitespace(operation, ref pos);
                    error = null;
                    c = GetChar(operation, pos);
                    if (c == '(') {
                        pos++;
                        return new Token(TokenType.Function, start + 1, str);
                    }
                    return new Token(TokenType.Symbol, start + 1, str);
                }
            }
            error = $"unexpected character: '{(char)c}' {At} {pos}";
            return new Token(TokenType.Error, pos);
        }
        
        private static Token GetNumber(bool negative, string operation, ref int pos, out string error) {
            int     start = pos++ - (negative ? 1 : 0);
            bool    isFloat = false;
            while (true) {
                int c = GetChar(operation, pos);
                if (IsDigit(c)) {
                    pos++;
                    continue;
                }
                if ('.' == c) {
                    pos++;
                    if (isFloat) {
                        var flt = operation.Substring(start, pos - start);
                        error = $"invalid floating point number: {flt} {At} {pos-1}";
                        return new Token(TokenType.Error, start);
                    }
                    isFloat = true;
                    continue;
                }
                break;
            }
            error = null;
            var str = operation.Substring(start, pos - start);
            if (isFloat) {
                double dbl = double.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                return new Token(dbl, start);
            }
            long lng = long.Parse(str);
            return new Token(lng, start);
        }
        
        private static Token GetString(string operation, int terminator, ref int pos, out string error) {
            int start = pos;
            bool hasEscapedChars = false;
            while (true) {
                int c = GetChar(operation, pos);
                switch (c) {
                    case End: {
                        var str = operation.Substring(start, pos - start);
                        error = $"missing string terminator for: {str} {At} {pos}";
                        return new Token(TokenType.Error, start);
                    }
                    case '\\':
                        hasEscapedChars = true;
                        c = GetChar(operation, ++pos);
                        if (c == End) {
                            var str = operation.Substring(start, pos - start);
                            error = $"missing escaped character for: {str} {At} {pos}";
                            return new Token(TokenType.Error, start);
                        }
                        pos++;
                        continue;
                }
                if (c == terminator) {
                    error = null;
                    var str = hasEscapedChars ?
                        UnEscape (operation, start, pos) :
                        operation.Substring(start, pos - start);
                    pos++;
                    return new Token(TokenType.String, start, str);
                }
                pos++;
            }
        }
        
        private static string UnEscape(string str, int start, int end) {
            var sb = new StringBuilder(end - start);
            for (int n = start; n < end; n++) {
                var c = str[n];
                if (c == '\\') {
                    c = str[++n];
                    switch (c) {
                        case 'b': sb.Append('\b'); continue;    // backspace
                        case 'f': sb.Append('\f'); continue;    // form feed
                        case 'n': sb.Append('\n'); continue;    // new line
                        case 'r': sb.Append('\r'); continue;    // carriage return
                        case 't': sb.Append('\t'); continue;    // horizontal tabulator
                        case 'v': sb.Append('\v'); continue;    // vertical tabulator
                    }
                    sb.Append(c);
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
        
        private static void SkipWhitespace(string operation, ref int pos) {
            while (true) {
                int c = GetChar(operation, pos);
                switch (c) {
                    case ' ':   case '\t':  case '\r':  case '\n':
                        pos++;
                        continue;
                }
                return;
            }
        }
    }
}