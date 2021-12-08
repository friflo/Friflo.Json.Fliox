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
            var node = QueryTree.CreateTree(result.items,out error);
            if (error != null)
                return null;
            var op = GetOperation (node,               out error);
            return op;
        }
        
        public static Operation OperationFromNode (QueryNode node, out string error) {
            var op      = GetOperation (node, out error);
            return op;
        }
        
        private static Operation GetOperation(QueryNode node, out string error) {
            BinaryOperands b;
            
            switch (node.operation.type)
            {
                // --- binary tokens
                case TokenType.Add:             b = Bin(node, out error);   return new Add                  (b.left, b.right);
                case TokenType.Sub:             b = Bin(node, out error);   return new Subtract             (b.left, b.right);
                case TokenType.Mul:             b = Bin(node, out error);   return new Multiply             (b.left, b.right);
                case TokenType.Div:             b = Bin(node, out error);   return new Divide               (b.left, b.right);
                //
                case TokenType.Greater:         b = Bin(node, out error);   return new GreaterThan          (b.left, b.right);
                case TokenType.GreaterOrEqual:  b = Bin(node, out error);   return new GreaterThanOrEqual   (b.left, b.right);
                case TokenType.Less:            b = Bin(node, out error);   return new LessThan             (b.left, b.right);
                case TokenType.LessOrEqual:     b = Bin(node, out error);   return new LessThanOrEqual      (b.left, b.right);
                case TokenType.Equals:          b = Bin(node, out error);   return new Equal                (b.left, b.right);
                case TokenType.NotEquals:       b = Bin(node, out error);   return new NotEqual             (b.left, b.right);
                
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
                    if (node.isFunction) {
                        return GetFunction(node, out error);
                    }
                    return GetField(node, out error);
            // no case TokenType.BracketOpen:  -> is never added as QueryNode
                default:
                    error = $"unexpected operation {node.operation}";
                    return null;
            }
        }
        
        private static Operation GetField(QueryNode node, out string error) {
            var symbol = node.operation.str;
            error = null;
            switch (symbol) {
                case "true":    return new TrueLiteral();
                case "false":   return new FalseLiteral();
                case "null":    return new NullLiteral();
                case "if":
                case "else":
                case "while":
                case "do":
                case "for":
                    error = $"operation must not use conditional statement: {symbol}";
                    return null;
            }
            return new Field(symbol);
        }
        
        private static Operation GetMathFunction(QueryNode node, out string error) {
            string      symbol  = node.operation.str;
            Operation   operand;
            switch (symbol) {
                case "Abs":     operand = Operand(node, out error);     return new Abs      (operand);
                case "Ceiling": operand = Operand(node, out error);     return new Ceiling  (operand);
                case "Floor":   operand = Operand(node, out error);     return new Floor    (operand);
                case "Exp":     operand = Operand(node, out error);     return new Exp      (operand);
                case "Log":     operand = Operand(node, out error);     return new Log      (operand);
                case "Sqrt":    operand = Operand(node, out error);     return new Sqrt     (operand);
                default:
                    error = $"unknown function: {symbol}()";
                    return null;
            }
        }
        
        private static Operation Operand(in QueryNode node, out string error) {
            if (node.OperandCount != 1) {
                error = $"operation {node.operation} expect one operand";
                return default;
            }
            error = null;
            var operand = node.GetOperand(0);
            return GetOperation(operand, out error);
        }
        
        private static Operation GetFunction(QueryNode node, out string error) {
            var symbol  = node.operation.str;
            var lastDot = symbol.LastIndexOf('.');
            if (lastDot == -1) {
                return GetMathFunction(node, out error);
            }
            string method   = symbol.Substring(lastDot + 1);
            string field    = symbol.Substring(0, lastDot);
            Aggregate       l;
            Quantify        q;
            BinaryOperands  b;
            switch (method) {
                // --- aggregate operations
                case "Min":     l = Aggregate(field, node, out error);  return new Min       (l.field, l.arg, l.operand);
                case "Max":     l = Aggregate(field, node, out error);  return new Max       (l.field, l.arg, l.operand);
                case "Sum":     l = Aggregate(field, node, out error);  return new Sum       (l.field, l.arg, l.operand);
                case "Average": l = Aggregate(field, node, out error);  return new Average   (l.field, l.arg, l.operand);
                
                // --- quantify  operations
                case "Any":     q = Quantify(field, node, out error);   return new Any       (q.field, q.arg, q.filter);
                case "All":     q = Quantify(field, node, out error);   return new All       (q.field, q.arg, q.filter);
                case "Count":   q = Quantify(field, node, out error);   return new CountWhere(q.field, q.arg, q.filter);
                
                // --- string operations
                case "Contains":    b = Params(field, node, out error); return new Contains  (b.left, b.right);
                case "StartsWith":  b = Params(field, node, out error); return new StartsWith(b.left, b.right);
                case "EndsWith":    b = Params(field, node, out error); return new EndsWith  (b.left, b.right);
                default:
                    error = $"unknown method: {method}() used by: {symbol}";
                    return null;
            }
        }
        
        private static Aggregate Aggregate(string fieldName, in QueryNode node, out string error) {
            error = null;
            var field       = new Field(fieldName);
            var argOperand  = node.GetOperand(0);
            var fcnOperand  = node.GetOperand(1);
            var arg         = argOperand.operation.str;
            var fcn         = GetOperation(fcnOperand, out error);
            return new Aggregate(field, arg, fcn);
        }
        
        private static Quantify Quantify(string fieldName, in QueryNode node, out string error) {
            var field       = new Field(fieldName);
            var argOperand  = node.GetOperand(0);
            var fcnOperand  = node.GetOperand(1);
            var arg         = argOperand.operation.str;
            var fcn         = GetOperation(fcnOperand, out error);
            if (fcn is FilterOperation filter) {
                error = null;
                return new Quantify(field, arg, filter);
            }
            error = $"quantify operation {node.operation}() expect boolean lambda body. Was: {fcn}";
            return default;
        }
        
        private static BinaryOperands Params(string fieldName, in QueryNode node, out string error) {
            error = null;
            var field       = new Field(fieldName);
            var fcnOperand  = node.GetOperand(0);
            var fcn         = GetOperation(fcnOperand, out error);
            return new BinaryOperands(field, fcn);
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
        internal readonly   Operation   left;
        internal readonly   Operation   right;
        
        internal BinaryOperands(Operation left, Operation right) {
            this.left   = left;
            this.right  = right;
        }
    }
    
    internal readonly struct Aggregate {
        internal readonly   Field       field;
        internal readonly   string      arg; 
        internal readonly   Operation   operand;
        
        internal Aggregate(Field field, string arg, Operation operand) {
            this.field      = field;
            this.arg        = arg;
            this.operand    = operand;
        }
    }
    
    internal readonly struct Quantify {
        internal readonly   Field           field;
        internal readonly   string          arg; 
        internal readonly   FilterOperation filter;
        
        internal Quantify(Field field, string arg, FilterOperation filter) {
            this.field      = field;
            this.arg        = arg;
            this.filter     = filter;
        }
    }
}