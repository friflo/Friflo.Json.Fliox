// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public static class QueryParser
    {
        public static Operation Parse (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            if (error != null)
                return null;
            if (result.items.Length == 0) {
                error = "operation string is empty";
                return null;
            }
            var node    = QueryTree.CreateTree(result.items,out error);
            var op      = GetOperation (node,               out error);
            return op;
        }
        
        public static Operation OperationFromNode (QueryNode node, out string error) {
            var op      = GetOperation (node, out error);
            return op;
        }
        
        private static Operation GetOperation(QueryNode node, out string error) {
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
            case TokenType.String:          error = null;   return new StringLiteral(node.operation.str);
            case TokenType.Double:          error = null;   return new DoubleLiteral(node.operation.dbl);
            case TokenType.Long:            error = null;   return new LongLiteral  (node.operation.lng);
            
            case TokenType.Symbol:
                error = null;
                var symbol = node.operation.str;
                switch (symbol) {
                    case "true":    return new TrueLiteral();
                    case "false":   return new FalseLiteral();
                    case "null":    return new NullLiteral();
                    case "if":
                    case "else":
                    case "while":
                    case "do":
                    case "for":
                        error = $"expression must not use conditional statement: {symbol}";
                        return null;
                    default:        return new Field(symbol);
                }
            
            // --- group tokens
            /*case TokenType.BracketOpen:
                pos++;
                if (op is Field) {}
                var first = GetOperation(tokens, ref pos, null, out error);
                
                break; */
            default:
                error = $"unexpected operation {node.operation}";
                return null;
            }
        }
        
        private static BinaryOperands Bin(in QueryNode node, out string error) {
            if (node.OperandCount != 2) {
                error = $"operation {node.operation} expect two operands";
                return default;
            }
            var operand_0 = node.GetOperand(0);
            var operand_1 = node.GetOperand(1);
            var left    = GetOperation(operand_0, out error);
            var right   = GetOperation(operand_1, out error);
            if (left is FilterOperation || right is FilterOperation) {
                error = $"operation {node.operation.ToString()} must not use boolean operands";
                return default;
            }
            return new BinaryOperands (left, right);
        }
        
        private static List<FilterOperation> FilterOperands(in QueryNode node, out string error) {
            if (node.OperandCount < 2) {
                error = $"expect at minimum two operands for operation {node.operation}";
                return null;
            }
            var operands = new List<FilterOperation> (node.OperandCount);
            for (int n = 0; n < node.OperandCount; n++) {
                var operand     = node.GetOperand(n);
                var operation   = GetOperation(operand, out error);
                if (error != null)
                    return null;
                if (operation is FilterOperation filterOperation) {
                    operands.Add(filterOperation);
                } else {
                    error = $"operation {node.operation} expect boolean operands. Got: {operation}";
                    return null;
                }
            }
            error = null;
            return operands;
        }
    }
    
    internal readonly struct BinaryOperands {
        internal readonly Operation left;
        internal readonly Operation right;
        
        internal BinaryOperands(Operation left, Operation right) {
            this.left   = left;
            this.right  = right;
        }
    }
}