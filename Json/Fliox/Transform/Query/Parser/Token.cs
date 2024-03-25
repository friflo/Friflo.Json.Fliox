// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

using static Friflo.Json.Fliox.Transform.Query.Parser.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public readonly struct TokenList
    {
        public  readonly Token[] items;
        
        public TokenList (Token[] items) {
            this.items = items;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var item in items) {
                sb.Append(item.GetLabel(false));
            }
            return sb.ToString();
        }
    }
    
    // ------------------------------------ Token ------------------------------------
    public readonly struct Token {
        internal readonly   TokenType   type;
        internal readonly   string      str;
        internal readonly   long        lng;
        internal readonly   double      dbl;
        internal readonly   int         pos;
        
        public   override   string      ToString() => GetLabel(true);

        internal Token (TokenType type, int position, string str = null) {
            this.type   = type;
            this.str    = str;
            this.lng    = 0;
            this.dbl    = 0;
            this.pos    = position - 1;
        }
        
        internal Token (long lng, int position) {
            this.type   = TokenType.Long;
            this.lng    = lng;
            this.str    = null;
            this.dbl    = 0;
            this.pos    = position;
        }

        internal Token (double dbl, int position) {
            this.type   = TokenType.Double;
            this.dbl    = dbl;
            this.lng    = 0;
            this.str    = null;
            this.pos    = position;
        }

        internal string GetLabel(bool decorate) {
            switch (type) {
                case TokenType.Symbol:          return str;
                case TokenType.Function:        return decorate ? $"{str}()" : $"{str}(";
                case TokenType.Long:            return lng.ToString();
                case TokenType.Double:          return dbl.ToString(CultureInfo.InvariantCulture);
                case TokenType.String:          return $"'{str}'";
                //
                case TokenType.Add:             return "+";
                case TokenType.Sub:             return "-";
                case TokenType.Mul:             return "*";
                case TokenType.Div:             return "/";
                case TokenType.Mod:             return "%";
                //
                case TokenType.Greater:         return ">";
                case TokenType.GreaterOrEqual:  return ">=";
                case TokenType.Less:            return "<";
                case TokenType.LessOrEqual:     return "<=";
                case TokenType.Not:             return "!";
                case TokenType.NotEquals:       return "!=";
                //
                case TokenType.BracketOpen:     return "(";
                case TokenType.BracketClose:    return ")";
                //
                case TokenType.Or:              return "||";
                case TokenType.And:             return "&&";
                //
                case TokenType.Equals:          return "==";
                case TokenType.Arrow:           return "=>";
                //
                case TokenType.Error:           return "ERROR";
                case TokenType.Whitespace:      return "WS";
                case TokenType.End:             return "END";
                //
                default:                        return "---";
            }
        }
        
        private  static readonly    TokenShape[]    TokenShapes;
        internal static             TokenShape      Shape(TokenType type) => TokenShapes[(int)type];
        
        static Token() {
            TokenShapes = CreateTokenShapes();
        }
        
        // [C# - Operators Precedence] https://www.tutorialspoint.com/csharp/csharp_operators_precedence.htm
        private static TokenShape[] CreateTokenShapes() {
            var tempShapes = new [] {
                //             name,                    arity,  precedence
                new TokenShape(TokenType.Mul,           Binary, 2),
                new TokenShape(TokenType.Div,           Binary, 2),
                new TokenShape(TokenType.Mod,           Binary, 2),
                new TokenShape(TokenType.Add,           Binary, 3),
                new TokenShape(TokenType.Sub,           Binary, 3),
                //
                new TokenShape(TokenType.Greater,       Binary, 4),
                new TokenShape(TokenType.GreaterOrEqual,Binary, 4),
                new TokenShape(TokenType.Less,          Binary, 4),
                new TokenShape(TokenType.LessOrEqual,   Binary, 4),
                new TokenShape(TokenType.NotEquals,     Binary, 5),
                new TokenShape(TokenType.Equals,        Binary, 5),
                //
                new TokenShape(TokenType.And,           NAry,   6),
                new TokenShape(TokenType.Or,            NAry,   7),
                //
                new TokenShape(TokenType.Symbol,        Unary,  8),
                new TokenShape(TokenType.Function,      Unary,  8),
                new TokenShape(TokenType.Long,          Unary,  8),
                new TokenShape(TokenType.Double,        Unary,  8),
                new TokenShape(TokenType.String,        Unary,  8),
                //
                // '(' require lowest precedence of all.
                // So enclosed n-ary operations (n>1) will always use preceding operands as their left operand.
                new TokenShape(TokenType.BracketOpen,   Undef,  9),
                new TokenShape(TokenType.BracketClose,  Undef,  1),
                new TokenShape(TokenType.Not,           Unary,  1), // todo
                //
                new TokenShape(TokenType.Arrow,         Binary, 10),
                //
                new TokenShape(TokenType.End,           Undef, -1),
                new TokenShape(TokenType.Start,         Undef, -1),
                new TokenShape(TokenType.Whitespace,    Undef, -1),
                new TokenShape(TokenType.Error,         Undef, -1),
            };
            var count = Enum.GetNames(typeof(TokenType)).Length;
            if (count != tempShapes.Length)
                throw new InvalidOperationException("Invalid token shape length");
            var shapes = new TokenShape[count];
            for (int n = 0; n < count; n++) {
                var shape = tempShapes[n];
                var index = (int)shape.type; 
                shapes[index] = shape;
            }
            for (int n = 0; n < count; n++) {
                var shape = shapes[n];
                var index = (int)shape.type;
                if (index != n)
                    throw new InvalidOperationException($"Invalid shape[{n}]. Was {shape.type}");
                shapes[index] = shape;
            }
            return shapes;
        }
    }
}