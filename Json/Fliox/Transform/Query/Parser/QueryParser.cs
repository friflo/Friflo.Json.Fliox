// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public static class QueryParser
    {
        public static Operation Parse (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            var node    = QueryTree.CreateTree(result.items,out error);
            var op      = GetOperation (node,               out error);
            return op;
        }
        
        public static Operation OperationFromNode (QueryNode node, out string error) {
            var op      = GetOperation (node, out error);
            return op;
        }
        
        private static Operation GetOperation(QueryNode node, out string error) {
            error = null;
            BinaryOperands bin;
            
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
            
            // --- arity tokens
            case TokenType.Or:
                var filterOperands  = FilterOperands(node, out error); 
                return new Or (filterOperands);
            case TokenType.And:
                filterOperands      = FilterOperands(node, out error); 
                return new And (filterOperands);

            // --- unary tokens
            case TokenType.String:          return new StringLiteral(node.operation.str);
            case TokenType.Double:          return new DoubleLiteral(node.operation.dbl);
            case TokenType.Long:            return new LongLiteral  (node.operation.lng);
            
            case TokenType.Symbol:
                var symbol = node.operation.str; 
                if (symbol == "true")    return new TrueLiteral();
                if (symbol == "false")   return new FalseLiteral();
                if (symbol == "null")    return new NullLiteral();
                return new Field(symbol);
            
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
        
        private static BinaryOperands Bin(in QueryNode node, out string error) {
            if (node.Count != 2) {
                error = "expect two operands";
                return new BinaryOperands();
            }
            error       = null;
            var left    = GetOperation(node[0], out error);
            var right   = GetOperation(node[1], out error);
            return new BinaryOperands { left = left, right = right };
        }
        
        private static List<FilterOperation> FilterOperands(in QueryNode node, out string error) {
            var operands = new List<FilterOperation> (node.Count);
            for (int n = 0; n < node.operands.Count; n++) {
                var operand = GetOperation(node[n], out error);
                if (operand is FilterOperation filterOperand) {
                    operands.Add(filterOperand);
                } else {
                    error = "invalid filter operand";
                    return null;
                }
            }
            error = null;
            return operands;
        }
        
        
        /* private Operation Error (string message, out string error) {
            error = message;
            return null;
        } */
    }
    
    internal struct BinaryOperands {
        internal Operation left;
        internal Operation right;
    }

}