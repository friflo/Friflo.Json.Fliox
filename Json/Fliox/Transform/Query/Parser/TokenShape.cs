// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    internal readonly struct TokenShape {
        internal readonly   TokenType   type;
        internal readonly   Arity       arity;
        internal readonly   int         precedence;

        public override string ToString() => $"{type,-14} arity: {arity}, precedence: {precedence}";

        internal TokenShape (TokenType type, Arity arity, int precedence) {
            this.type       = type;
            this.arity      = arity;
            this.precedence = precedence;
        }
    }
    
    internal enum Arity {
        Undef,
        Unary,
        Binary,
        NAry
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
        Whitespace,
        End,
    }
}