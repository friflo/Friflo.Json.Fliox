// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public readonly struct TokenShape {
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
        Start,          // initial lastType - not added to token[] result  
        End,            // End of string    - not added to token[] result 
        Whitespace,     // whitespace       - not added to token[] result  
        Error,          // Lexer error      - token[] result is null
        //
        Symbol,         //   o.name  o.child.name  true  false  null
        Function,       //   o.name.StartsWith(    o.items.Any(  Abs(    
        /// ( and ) are used for functions or grouping operations
        BracketOpen,    //   ( 
        /// <see cref="BracketClose"/> must not be used to create a <see cref="QueryNode"/> 
        BracketClose,   //   )
        //
        Long,           //   1
        Double,         //   1.2
        String,         //   'abc' "abc"
        //
        Add,            //   +
        Sub,            //   -
        Mul,            //   *
        Div,            //   /
        Mod,            //   %
        //
        Greater,        //   >
        GreaterOrEqual, //   >=
        Less,           //   <
        LessOrEqual,    //   <=
        Not,            //   !
        NotEquals,      //   !=
        //
        Or,             //   ||
        And,            //   &&
        //
        Equals,         //   ==
        Arrow,          //   =>
    }
}