// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    // ------------------------------------ Token ------------------------------------
    public readonly struct TokenList
    {
        public  readonly Token[] items;
        
        public TokenList (Token[] items) {
            this.items = items;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var item in items) {
                sb.Append(item.ToString());
            }
            return sb.ToString();
        }
    }
    
    public readonly struct Token {
        public  readonly    TokenType   type;
        public  readonly    string      str;
        public  readonly    long        lng;
        public  readonly    double      dbl;

        internal Token (TokenType type, string str = null) {
            this.type   = type;
            this.str    = str;
            this.lng    = 0;
            this.dbl    = 0;
        }
        
        internal Token (long lng) {
            this.type   = TokenType.Long;
            this.lng    = lng;
            this.str    = null;
            this.dbl    = 0;
        }

        internal Token (double dbl) {
            this.type   = TokenType.Double;
            this.dbl    = dbl;
            this.lng    = 0;
            this.str    = null;
        }

        public override string ToString() {
            switch (type) {
                case TokenType.Symbol:  return str;
                case TokenType.Long:    return lng.ToString();
                case TokenType.Double:  return dbl.ToString(CultureInfo.InvariantCulture);
                case TokenType.String:  return $"'{str}'";
                //
                case TokenType.Add:             return "+";
                case TokenType.Sub:             return "-";
                case TokenType.Mul:             return "*";
                case TokenType.Div:             return "/";
                //
                case TokenType.Greater:         return ">";
                case TokenType.GreaterOrEqual:  return ">=";
                case TokenType.Less:            return "<";
                case TokenType.LessOrEqual:     return "<=";
                case TokenType.Not:             return "!";
                case TokenType.NotEquals:       return "!=";
                //
                case TokenType.Dot:             return ".";
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
                //             name,             operands, precedence
                new TokenShape(TokenType.Mul,           2, 2),
                new TokenShape(TokenType.Div,           2, 2),
                new TokenShape(TokenType.Add,           2, 3),
                new TokenShape(TokenType.Sub,           2, 3),
                //
                new TokenShape(TokenType.Greater,       2, 4),
                new TokenShape(TokenType.GreaterOrEqual,2, 4),
                new TokenShape(TokenType.Less,          2, 4),
                new TokenShape(TokenType.LessOrEqual,   2, 4),
                new TokenShape(TokenType.NotEquals,     2, 5),
                new TokenShape(TokenType.Equals,        2, 5),
                //
                new TokenShape(TokenType.And,          -1, 6),
                new TokenShape(TokenType.Or,           -1, 7),
                //
                new TokenShape(TokenType.Symbol,        1, 1),
                new TokenShape(TokenType.Long,          1, 1),
                new TokenShape(TokenType.Double,        1, 1),
                new TokenShape(TokenType.String,        1, 1),
                //
                new TokenShape(TokenType.BracketOpen,   1, 1),
                new TokenShape(TokenType.BracketClose,  1, 1),
                new TokenShape(TokenType.Not,           1, 1),
                new TokenShape(TokenType.Dot,           1, 1),
                //
                new TokenShape(TokenType.Arrow,         2, 1),
                new TokenShape(TokenType.Error,         0,-1),
                new TokenShape(TokenType.End,           0,-1),
                new TokenShape(TokenType.Start,         0,-1),
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
    
    internal readonly struct TokenShape {
        internal readonly   TokenType   type;
        internal readonly   int         operands;
        internal readonly   int         precedence;

        public override string ToString() => $"{type,-14} operands: {operands}, precedence: {precedence}";

        internal TokenShape (TokenType type, int operands, int precedence) {
            this.type       = type;
            this.operands   = operands;
            this.precedence = precedence;
        }
    }
    
    public enum TokenType
    {
        Start,
        //
        Symbol,
        Long,
        Double,
        String,
        //
        Add,            // +
        Sub,            // -
        Mul,            // *
        Div,            // /
        //
        Greater,        // >
        GreaterOrEqual, // >=
        Less,           // <
        LessOrEqual,    // <=
        Not,            // !
        NotEquals,      // !=
        //
        Dot,            // .
        BracketOpen,    // (
        BracketClose,   // )
        //
        Or,             // ||
        And,            // &&
        //
        Equals,         // ==
        Arrow,          // =>
        //
        Error,
        End,
    }
}