// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
        
        // [C# - Operators Precedence] https://www.tutorialspoint.com/csharp/csharp_operators_precedence.htm
        internal int Precedence(TokenType type) {
            switch (type) {
                case TokenType.Symbol:          return -1;
                case TokenType.Long:            return -1;
                case TokenType.Double:          return -1;
                case TokenType.String:          return -1;
                case TokenType.Not:             return -1;
                //
                case TokenType.Mul:             return  2;
                case TokenType.Div:             return  2;
                case TokenType.Add:             return  1;
                case TokenType.Sub:             return  1;
                //
                case TokenType.Greater:         return  2;
                case TokenType.GreaterOrEqual:  return  2;
                case TokenType.Less:            return  2;
                case TokenType.LessOrEqual:     return  2;
                case TokenType.NotEquals:       return  1;
                case TokenType.Equals:          return  1;
                //
                case TokenType.BracketOpen:     return  3;
                case TokenType.BracketClose:    return -1;
                case TokenType.Dot:             return -1;
                //
                case TokenType.And:             return  2;
                case TokenType.Or:              return  1;
                //
                case TokenType.Arrow:           return -1;
                //
                case TokenType.Error:           return -1;
                case TokenType.End:             return -1;
                //
                default:                        return 0;
            }
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