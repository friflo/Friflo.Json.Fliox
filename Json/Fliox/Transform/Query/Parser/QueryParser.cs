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
        /// <summary>
        /// namespace, class or method name may change. Use <see cref="Operation.Parse"/> instead.
        /// <br/>
        /// Traverse the tree returned by <see cref="QueryTree.CreateTree"/> and create itself a tree of
        /// <see cref="Operation"/>'s while visiting the given tree.
        /// </summary>
        public static Operation Parse (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            if (error != null)
                return null;
            if (result.items.Length == 0) {
                error = "operation is empty";
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
            BinaryOperands          b;
            List<FilterOperation>   f;
            
            switch (node.operation.type)
            {
                // --- binary tokens
                case TokenType.Add:             b = Bin(node, false, out error);    return new Add                  (b.left, b.right);
                case TokenType.Sub:             b = Bin(node, false, out error);    return new Subtract             (b.left, b.right);
                case TokenType.Mul:             b = Bin(node, false, out error);    return new Multiply             (b.left, b.right);
                case TokenType.Div:             b = Bin(node, false, out error);    return new Divide               (b.left, b.right);
                //
                case TokenType.Greater:         b = Bin(node, false, out error);    return new GreaterThan          (b.left, b.right);
                case TokenType.GreaterOrEqual:  b = Bin(node, false, out error);    return new GreaterThanOrEqual   (b.left, b.right);
                case TokenType.Less:            b = Bin(node, false, out error);    return new LessThan             (b.left, b.right);
                case TokenType.LessOrEqual:     b = Bin(node, false, out error);    return new LessThanOrEqual      (b.left, b.right);
                case TokenType.Equals:          b = Bin(node, true,  out error);    return new Equal                (b.left, b.right);
                case TokenType.NotEquals:       b = Bin(node, true,  out error);    return new NotEqual             (b.left, b.right);
                
                // --- arity tokens
                case TokenType.Or:          f = FilterOperands(node, out error);    return new Or (f);
                case TokenType.And:         f = FilterOperands(node, out error);    return new And (f);

                // --- unary tokens
                case TokenType.Not:         return NotOp(node, out error);
                case TokenType.String:      error = null;   return new StringLiteral(node.operation.str);
                case TokenType.Double:      error = null;   return new DoubleLiteral(node.operation.dbl);
                case TokenType.Long:        error = null;   return new LongLiteral  (node.operation.lng);
                
                case TokenType.Symbol:      return GetField     (node, out error);
                case TokenType.Function:    return GetFunction  (node, out error);
                case TokenType.BracketOpen: return GetScope     (node, out error);
                default:
                    error = $"unexpected operation {node.operation} {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation GetScope(QueryNode node, out string error) {
            if (node.OperandCount != 1) {
                error = $"parentheses (...) expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0   = node.GetOperand(0);
            var operation   = GetOperation(operand_0, out error);
            return operation;
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
                    error = $"conditional statements must not be used: {symbol} {At} {node.Pos}";
                    return null;
            }
            if (node.isLambda) {
                var arrowNode = node.GetOperand(0); // is always present as it isLambda
                if (arrowNode.OperandCount != 1) {
                    error = $"lambda '{node.operation} =>' expect one subsequent operand as body {At} {arrowNode.Pos}";
                    return default;
                }
                var body    = arrowNode.GetOperand(0);
                var bodyOp  = GetOperation(body, out error);
                return new Lambda(symbol, bodyOp);
            }
            return new Field(symbol);
        }
        
        private static Operation GetMathFunction(QueryNode node, out string error) {
            string      symbol  = node.operation.str;
            Operation   operand;
            switch (symbol) {
                case "Abs":     operand = FcnOp(node, out error);     return new Abs      (operand);
                case "Ceiling": operand = FcnOp(node, out error);     return new Ceiling  (operand);
                case "Floor":   operand = FcnOp(node, out error);     return new Floor    (operand);
                case "Exp":     operand = FcnOp(node, out error);     return new Exp      (operand);
                case "Log":     operand = FcnOp(node, out error);     return new Log      (operand);
                case "Sqrt":    operand = FcnOp(node, out error);     return new Sqrt     (operand);
                default:
                    error = $"unknown function: {symbol}() {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation FcnOp(in QueryNode node, out string error) {
            if (node.OperandCount != 1) {
                error = $"function {node.operation} expect one operand {At} {node.Pos}";
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
            if (field.IndexOf('.') == -1) {
                error = $"expect . in field name {field} {At} {node.Pos}";
                return null;
            }
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
                case "Contains":    b = StringOp(field, node, out error); return new Contains  (b.left, b.right);
                case "StartsWith":  b = StringOp(field, node, out error); return new StartsWith(b.left, b.right);
                case "EndsWith":    b = StringOp(field, node, out error); return new EndsWith  (b.left, b.right);
                default:
                    error = $"unknown method: {method}() used by: {symbol} {At} {node.Pos}";
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
            error = $"quantify operation {node.operation} expect boolean lambda body. Was: {fcn} {At} {fcnOperand.Pos}";
            return default;
        }
        
        private static BinaryOperands StringOp(string fieldName, in QueryNode node, out string error) {
            var field       = new Field(fieldName);
            var fcnOperand  = node.GetOperand(0);
            var fcn         = GetOperation(fcnOperand, out error);
            if (fcn is StringLiteral || fcn is Field) {
                error = null;
                return new BinaryOperands(field, fcn);
            }
            error = $"expect string or field operand in {node.operation}. was: {fcnOperand} {At} {fcnOperand.Pos}";
            return default;
        }
        
        private static Not NotOp(in QueryNode node, out string error) {
            if (node.OperandCount != 1) {
                error = $"not operator expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0  = node.GetOperand(0);
            var operand    = GetOperation(operand_0, out error);
            if (operand is FilterOperation filterOperand) {
                return new Not(filterOperand);
            }
            error = $"not operator ! must use a boolean operand. Was: {operand_0.operation} {At} {operand_0.Pos}";
            return default;
        }

        private static BinaryOperands Bin(in QueryNode node, bool boolOperands, out string error) {
            if (node.OperandCount != 2) {
                error = $"operator {node.operation} expect two operands {At} {node.Pos}";
                return default;
            }
            var operand_0 = node.GetOperand(0);
            var operand_1 = node.GetOperand(1);
            var left    = GetOperation(operand_0, out error);
            if (error != null)
                return default;
            var right   = GetOperation(operand_1, out error);
            if (error != null)
                return default;
            if (boolOperands)
                return new BinaryOperands (left, right);
            if (left is FilterOperation || right is FilterOperation) {
                error = $"operator {node.operation.ToString()} must not use boolean operands {At} {node.Pos}";
                return default;
            }
            return new BinaryOperands (left, right);
        }
        
        private static List<FilterOperation> FilterOperands(in QueryNode node, out string error) {
            if (node.OperandCount < 2) {
                error = $"expect at minimum two operands for operator {node.operation} {At} {node.Pos}";
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
                    error = $"operator {node.operation} expect boolean operands. Got: {operation} {At} {operand.Pos}";
                    return null;
                }
            }
            error = null;
            return operands;
        }
        
        private const string At = "at pos";
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