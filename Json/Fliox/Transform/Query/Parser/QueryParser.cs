// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    internal readonly struct Context
    {
        internal readonly List<string> variables;
        
        internal Context(List<string> variables) {
            this.variables = variables ?? new List<string>();
        }
    }
    
    public static class QueryParser
    {
        /// <summary>
        /// namespace, class or method name may change. Use <see cref="Operation.Parse"/> instead.
        /// <br/>
        /// Traverse the tree returned by <see cref="QueryTree.CreateTree"/> and create itself a tree of
        /// <see cref="Operation"/>'s while visiting the given tree.
        /// </summary>
        public static Operation Parse (string operation, out string error, List<string> variables = null) {
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
            var cx  = new Context (variables);
            var op  = GetOperation (node, cx, out error);
            return op;
        }
        
        public static Operation OperationFromNode (QueryNode node, out string error) {
            var cx  = new Context (null);
            var op  = GetOperation (node, cx, out error);
            return op;
        }
        
        private static Operation GetOperation(QueryNode node, in Context cx, out string error) {
            BinaryOperands          b;
            List<FilterOperation>   f;
            
            switch (node.TokenType)
            {
                // --- binary tokens
                case TokenType.Add:             b = Bin(node, cx, false, out error);    return new Add                  (b.left, b.right);
                case TokenType.Sub:             b = Bin(node, cx, false, out error);    return new Subtract             (b.left, b.right);
                case TokenType.Mul:             b = Bin(node, cx, false, out error);    return new Multiply             (b.left, b.right);
                case TokenType.Div:             b = Bin(node, cx, false, out error);    return new Divide               (b.left, b.right);
                //
                case TokenType.Greater:         b = Bin(node, cx, false, out error);    return new GreaterThan          (b.left, b.right);
                case TokenType.GreaterOrEqual:  b = Bin(node, cx, false, out error);    return new GreaterThanOrEqual   (b.left, b.right);
                case TokenType.Less:            b = Bin(node, cx, false, out error);    return new LessThan             (b.left, b.right);
                case TokenType.LessOrEqual:     b = Bin(node, cx, false, out error);    return new LessThanOrEqual      (b.left, b.right);
                case TokenType.Equals:          b = Bin(node, cx, true,  out error);    return new Equal                (b.left, b.right);
                case TokenType.NotEquals:       b = Bin(node, cx, true,  out error);    return new NotEqual             (b.left, b.right);
                
                // --- arity tokens
                case TokenType.Or:          f = FilterOperands(node, cx, out error);    return new Or (f);
                case TokenType.And:         f = FilterOperands(node, cx, out error);    return new And (f);

                // --- unary tokens
                case TokenType.Not:         return NotOp(node, cx, out error);
                case TokenType.String:      error = null;   return new StringLiteral(node.Str);
                case TokenType.Double:      error = null;   return new DoubleLiteral(node.Dbl);
                case TokenType.Long:        error = null;   return new LongLiteral  (node.Lng);
                
                case TokenType.Symbol:      return GetField     (node, cx, out error);
                case TokenType.Function:    return GetFunction  (node, cx, out error);
                case TokenType.BracketOpen: return GetScope     (node, cx, out error);
                default:
                    error = $"unexpected operation {node.operation} {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation GetScope(QueryNode node, in Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"parentheses (...) expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0   = node.GetOperand(0);
            var operation   = GetOperation(operand_0, cx, out error);
            return operation;
        }
        
        /// Arrow operands are added exclusively by <see cref="QueryTree.HandleArrow"/> 
        private static bool GetArrowBody(QueryNode node, int operandIndex, out QueryNode bodyNode, out string error) {
            var arrowNode = node.GetOperand(operandIndex);
            if (arrowNode.TokenType != TokenType.Arrow)
                throw new InvalidOperationException("expect Arrow node as operand");
            if (arrowNode.OperandCount != 1) {
                error = $"lambda '{node.operation} =>' expect one subsequent operand as body {At} {arrowNode.Pos}";
                bodyNode = null;
                return false;
            }
            error   = null;
            bodyNode    = arrowNode.GetOperand(0);
            return true;
        }
        
        private static Operation GetField(QueryNode node, in Context cx, out string error) {
            var symbol = node.Str;
            switch (symbol) {
                case "true":    error = null;   return new TrueLiteral();
                case "false":   error = null;   return new FalseLiteral();
                case "null":    error = null;   return new NullLiteral();
                case "if":
                case "else":
                case "while":
                case "do":
                case "for":
                    error = $"conditional statements must not be used: {symbol} {At} {node.Pos}";
                    return null;
            }
            if (node.OperandCount == 1) {
                if (!GetArrowBody(node, 0, out QueryNode bodyNode, out error))
                    return null;
                error = null;
                cx.variables.Add(node.Str);
                var bodyOp  = GetOperation(bodyNode, cx, out error);
                return new Lambda(symbol, bodyOp);
            }
            var firstDot = symbol.IndexOf('.');
            if (firstDot > 0) {
                var variable = symbol.Substring(0, firstDot);
                if (cx.variables.IndexOf(variable) == -1) {
                    error = $"variable '{variable}' not found {At} {node.Pos}";
                    return null;
                }
            }
            error = null;
            return new Field(symbol);
        }
        
        private static Operation GetMathFunction(QueryNode node, in Context cx, out string error) {
            string      symbol  = node.Str;
            Operation   operand;
            switch (symbol) {
                case "Abs":     operand = FcnOp(node, cx, out error);     return new Abs      (operand);
                case "Ceiling": operand = FcnOp(node, cx, out error);     return new Ceiling  (operand);
                case "Floor":   operand = FcnOp(node, cx, out error);     return new Floor    (operand);
                case "Exp":     operand = FcnOp(node, cx, out error);     return new Exp      (operand);
                case "Log":     operand = FcnOp(node, cx, out error);     return new Log      (operand);
                case "Sqrt":    operand = FcnOp(node, cx, out error);     return new Sqrt     (operand);
                default:
                    error = $"unknown function: {symbol}() {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation FcnOp(in QueryNode node, in Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"function {node.operation} expect one operand {At} {node.Pos}";
                return default;
            }
            error = null;
            var operand = node.GetOperand(0);
            return GetOperation(operand, cx, out error);
        }
        
        private static Operation GetFunction(QueryNode node, in Context cx, out string error) {
            var symbol  = node.Str;
            var lastDot = symbol.LastIndexOf('.');
            if (lastDot == -1) {
                return GetMathFunction(node, cx, out error);
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
                case "Min":     l = Aggregate(field, node, cx, out error);  return new Min       (l.field, l.arg, l.operand);
                case "Max":     l = Aggregate(field, node, cx, out error);  return new Max       (l.field, l.arg, l.operand);
                case "Sum":     l = Aggregate(field, node, cx, out error);  return new Sum       (l.field, l.arg, l.operand);
                case "Average": l = Aggregate(field, node, cx, out error);  return new Average   (l.field, l.arg, l.operand);
                
                // --- quantify  operations
                case "Any":     q = Quantify(field, node, cx, out error);   return new Any       (q.field, q.arg, q.filter);
                case "All":     q = Quantify(field, node, cx, out error);   return new All       (q.field, q.arg, q.filter);
                case "Count":   q = Quantify(field, node, cx, out error);   return new CountWhere(q.field, q.arg, q.filter);
                
                // --- string operations
                case "Contains":    b = StringOp(field, node, cx, out error); return new Contains  (b.left, b.right);
                case "StartsWith":  b = StringOp(field, node, cx, out error); return new StartsWith(b.left, b.right);
                case "EndsWith":    b = StringOp(field, node, cx, out error); return new EndsWith  (b.left, b.right);
                default:
                    error = $"unknown method: {method}() used by: {symbol} {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Aggregate Aggregate(string fieldName, in QueryNode node, Context cx, out string error) {
            error = null;
            var field       = new Field(fieldName);
            var argOperand  = node.GetOperand(0);
            if (!GetArrowBody(node, 1, out QueryNode bodyNode, out error))
                return default;
            cx.variables.Add(argOperand.Str);
            var bodyOp      = GetOperation(bodyNode, cx, out error);
            var arg         = argOperand.Str;
            return new Aggregate(field, arg, bodyOp);
        }
        
        private static Quantify Quantify(string fieldName, in QueryNode node, Context cx, out string error) {
            var field       = new Field(fieldName);
            var argOperand  = node.GetOperand(0);
            if (!GetArrowBody(node, 1, out QueryNode bodyNode, out error))
                return default;
            cx.variables.Add(argOperand.Str);
            var fcn         = GetOperation(bodyNode, cx, out error);
            if (fcn is FilterOperation filter) {
                error = null;
                var arg         = argOperand.Str;
                return new Quantify(field, arg, filter);
            }
            error = $"quantify operation {node.operation} expect boolean lambda body. Was: {fcn} {At} {bodyNode.Pos}";
            return default;
        }
        
        private static BinaryOperands StringOp(string fieldName, in QueryNode node, Context cx, out string error) {
            var field       = new Field(fieldName);
            var fcnOperand  = node.GetOperand(0);
            var fcn         = GetOperation(fcnOperand, cx, out error);
            if (fcn is StringLiteral || fcn is Field) {
                error = null;
                return new BinaryOperands(field, fcn);
            }
            error = $"expect string or field operand in {node.operation}. was: {fcnOperand} {At} {fcnOperand.Pos}";
            return default;
        }
        
        private static Not NotOp(in QueryNode node, Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"not operator expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0  = node.GetOperand(0);
            var operand    = GetOperation(operand_0, cx, out error);
            if (operand is FilterOperation filterOperand) {
                return new Not(filterOperand);
            }
            error = $"not operator ! must use a boolean operand. Was: {operand_0.operation} {At} {operand_0.Pos}";
            return default;
        }

        private static BinaryOperands Bin(in QueryNode node, Context cx, bool boolOperands, out string error) {
            if (node.OperandCount != 2) {
                error = $"operator {node.operation} expect two operands {At} {node.Pos}";
                return default;
            }
            var operand_0 = node.GetOperand(0);
            var operand_1 = node.GetOperand(1);
            var left    = GetOperation(operand_0, cx, out error);
            if (error != null)
                return default;
            var right   = GetOperation(operand_1, cx, out error);
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
        
        private static List<FilterOperation> FilterOperands(in QueryNode node, Context cx, out string error) {
            if (node.OperandCount < 2) {
                error = $"expect at minimum two operands for operator {node.operation} {At} {node.Pos}";
                return null;
            }
            var operands = new List<FilterOperation> (node.OperandCount);
            for (int n = 0; n < node.OperandCount; n++) {
                var operand     = node.GetOperand(n);
                var operation   = GetOperation(operand, cx, out error);
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