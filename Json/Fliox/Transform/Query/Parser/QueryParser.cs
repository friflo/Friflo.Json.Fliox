// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryParser
    {
        public Operation Parse (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            var node    = QueryTree.CreateTree(result.items,out error);
            var op      = GetOperation (node,               out error);
            return op;
        }
        
        private static Operation GetOperation(Node node, out string error) {
            error = null;
            BinsaryOperands bin;
            
            switch (node.operation.type)
            {
            // --- binary tokens
            case TokenType.Add:             bin = Bin(node, out error); return new Add                  (bin.left, bin.right);
            case TokenType.Sub:             bin = Bin(node, out error); return new Subtract             (bin.left, bin.right);
            case TokenType.Mul:             bin = Bin(node, out error); return new Multiply             (bin.left, bin.right);
            case TokenType.Div:             bin = Bin(node, out error); return new Divide               (bin.left, bin.right);
            //
            case TokenType.Greater:         bin = Bin(node, out error); return new GreaterThan          (bin.left, bin.right);
            case TokenType.GreaterOrEqual:  bin = Bin(node, out error); return new GreaterThanOrEqual   (bin.left, bin.right);
            case TokenType.Less:            bin = Bin(node, out error); return new LessThan             (bin.left, bin.right);
            case TokenType.LessOrEqual:     bin = Bin(node, out error); return new LessThanOrEqual      (bin.left, bin.right);
            case TokenType.Equals:          bin = Bin(node, out error); return new Equal                (bin.left, bin.right);
            case TokenType.NotEquals:       bin = Bin(node, out error); return new NotEqual             (bin.left, bin.right);
            
            // --- unary tokens
            case TokenType.String:          return new StringLiteral(node.operation.str);
            case TokenType.Double:          return new DoubleLiteral(node.operation.dbl);
            case TokenType.Long:            return new LongLiteral  (node.operation.lng);
            
            case TokenType.Symbol:
                if (node.operation.str == "true")    return new TrueLiteral();
                if (node.operation.str == "false")   return new FalseLiteral();
                if (node.operation.str == "null")    return new NullLiteral();
                error = $"unexpected symbol: {node.operation.str}";
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
        
        private static BinsaryOperands Bin(in Node node, out string error) {
            if (node.Count != 2) {
                error = "expect two operands";
                return new BinsaryOperands();
            }
            error       = null;
            var left    = GetOperation(node[0], out error);
            var right   = GetOperation(node[1], out error);
            return new BinsaryOperands { left = left, right = right };
        }
        
        /* private Operation Error (string message, out string error) {
            error = message;
            return null;
        } */
    }
    
    internal struct BinsaryOperands {
        internal Operation left;
        internal Operation right;
    }

}