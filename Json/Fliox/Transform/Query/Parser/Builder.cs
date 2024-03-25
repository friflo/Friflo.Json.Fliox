// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Transform.Query.Ops;

using TT = Friflo.Json.Fliox.Transform.Query.Parser.TokenType;
using static Friflo.Json.Fliox.Transform.Query.Parser.OperandType;

// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    /// <summary>
    /// Schematic processing overview
    /// <code>
    ///  operation string          token[]           node tree            operation tree
    /// ------------------> Lexer ---------> Parser -----------> Builder ---------------->
    /// </code>
    /// </summary>
    public static class QueryBuilder
    {
        /// <summary>
        /// namespace, class or method name may change. Use <see cref="Operation.Parse"/> instead.
        /// <br/>
        /// Traverse the node tree returned by <see cref="QueryParser.CreateTree"/> and create itself a tree of
        /// <see cref="Operation"/>'s.
        /// <returns>An <see cref="Operation"/> is successful.
        /// Otherwise it returns null and provide an descriptive <paramref name="error"/> message.</returns>
        /// </summary>
        public static Operation Parse (string operation, out string error, QueryEnv env = null) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            if (error != null)
                return null;
            if (result.items.Length == 0) {
                error = "operation is empty";
                return null;
            }
            var node = QueryParser.CreateTreeIntern(result.items,out error);
            if (error != null)
                return null;
            var cx  = new Context (env);
            return GetOperation (node, cx, out error);
        }
        
        public static Operation OperationFromNode (QueryNode node, out string error, QueryEnv env = null) {
            var cx  = new Context (env);
            return GetOperation (node, cx, out error);
        }
        
        private static bool GetOp(QueryNode node, Context cx, out Operation op, out string error) {
            op = GetOperation(node, cx, out error);
            return error == null;
        }
        
        private static Operation GetOperation(QueryNode node, Context cx, out string error) {
            BinaryOperands          b;
            List<FilterOperation>   f;
            
            switch (node.TokenType)
            {
                // --- binary tokens
                case TT.Add:            b = Bin(node, cx, Num,     out error);  return new Add              (b.left, b.right);
                case TT.Sub:            b = Bin(node, cx, Num,     out error);  return new Subtract         (b.left, b.right);
                case TT.Mul:            b = Bin(node, cx, Num,     out error);  return new Multiply         (b.left, b.right);
                case TT.Div:            b = Bin(node, cx, Num,     out error);  return new Divide           (b.left, b.right);
                case TT.Mod:            b = Bin(node, cx, Num,     out error);  return new Modulo           (b.left, b.right);
                //
                case TT.Greater:        b = Bin(node, cx, Num|Str, out error);  return new Greater          (b.left, b.right);
                case TT.GreaterOrEqual: b = Bin(node, cx, Num|Str, out error);  return new GreaterOrEqual   (b.left, b.right);
                case TT.Less:           b = Bin(node, cx, Num|Str, out error);  return new Less             (b.left, b.right);
                case TT.LessOrEqual:    b = Bin(node, cx, Num|Str, out error);  return new LessOrEqual      (b.left, b.right);
                case TT.Equals:         b = Bin(node, cx, Var,     out error);  return new Equal            (b.left, b.right);
                case TT.NotEquals:      b = Bin(node, cx, Var,     out error);  return new NotEqual         (b.left, b.right);
                
                // --- n-ary tokens
                case TT.Or:             f = FilterOps(node, cx, out error);     return new Or (f);
                case TT.And:            f = FilterOps(node, cx, out error);     return new And (f);

                // --- unary tokens
                case TT.Not:            return NotOp(node, cx, out error);
                
                // --- nullary tokens
                case TT.String:         Nullary(node, out error);               return new StringLiteral(node.ValueStr);
                case TT.Double:         Nullary(node, out error);               return new DoubleLiteral(node.ValueDbl);
                case TT.Long:           Nullary(node, out error);               return new LongLiteral  (node.ValueLng);
                
                case TT.Symbol:         return GetSymbolOp  (node, cx, out error);
                case TT.Function:       return GetFunction  (node, cx, out error);
                case TT.BracketOpen:    return GetScope     (node, cx, out error);
                default:
                    error = $"unexpected operation {node.Label} {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation GetScope(QueryNode node, Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"parentheses (...) expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0   = node.GetOperand(0);
            return GetOperation(operand_0, cx, out error);
        }
        
        /// Arrow operands are added exclusively by <see cref="QueryParser.HandleArrow"/> 
        private static bool GetArrowBody(QueryNode node, int operandIndex, out QueryNode bodyNode, out string error) {
            if (operandIndex >= node.OperandCount) {
                error = $"Invalid lambda expression in {node.Label} {At} {node.Pos}";
                bodyNode = null;
                return false;
            }
            var arrowNode = node.GetOperand(operandIndex);
            if (arrowNode.TokenType != TT.Arrow)
                throw new InvalidOperationException("expect Arrow node as operand");
            if (arrowNode.OperandCount != 1) {
                error = $"lambda '{node.Label} =>' expect lambda body {At} {arrowNode.Pos}";
                bodyNode = null;
                return false;
            }
            error   = null;
            bodyNode    = arrowNode.GetOperand(0);
            return true;
        }
        
        private static Operation GetSymbolOp(QueryNode node, Context cx, out string error) {
            var symbol = node.ValueStr;
            switch (symbol) {
                case "true":    Nullary(node, out error);   return new TrueLiteral();
                case "false":   Nullary(node, out error);   return new FalseLiteral();
                case "null":    Nullary(node, out error);   return new NullLiteral();
                case "E":       Nullary(node, out error);   return new EulerLiteral();
                case "PI":      Nullary(node, out error);   return new PiLiteral();
                case "Tau":     Nullary(node, out error);   return new TauLiteral();
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
                if (!cx.AddParameter(node, out error))
                    return null;
                if (!GetOp(bodyNode, cx, out var bodyOp, out error))
                    return null;
                if (bodyOp is FilterOperation filter) {
                    return Success(new Filter(symbol, filter), out error);
                }
                return Success(new Lambda(symbol, bodyOp), out error);
            }
            return CreateVariable(symbol, node, cx, out error);
        }
        
        private static Operation CreateVariable(string symbol, QueryNode node, Context cx, out string error) {
            var firstDot = symbol.IndexOf('.');
            if (firstDot != -1) {
                CreateField(symbol, node, cx, out Field field, out error);
                return field;
            }
            var findResult = cx.FindVariable(symbol); 
            switch (findResult.type) {
                case VariableType.NotFound:
                    error = $"variable not found: {symbol} {At} {node.Pos}";
                    return null;
                case VariableType.Parameter:
                    CreateField(symbol, node, cx, out Field field, out error);
                    return field;
                case VariableType.Variable:
                    return Success(findResult.value, out error);
            }
            throw new InvalidOperationException($"unexpected VariableType: {findResult.type}");
        }
        
        private static bool CreateField(string symbol, QueryNode node, Context cx, out Field field, out string error) {
            var firstDot = symbol.IndexOf('.');
            if (firstDot == 0 || symbol.Length == 0) {
                error = $"missing preceding variable for {node.Label} {At} {node.Pos}";
                field = null;
                return false;
            }
            if (firstDot == -1) {
                var findSymbol = cx.FindVariable(symbol); 
                if (findSymbol.type == VariableType.NotFound) {
                    error = $"variable not found: {symbol} {At} {node.Pos}";
                    field = null;
                    return false;
                }
                field = new Field(symbol);
                return Success(true, out error);
            }
            var param           = symbol.Substring(0, firstDot);
            var findVariable    = cx.FindVariable(param); 
            if (findVariable.type != VariableType.Parameter) {
                error = $"variable not found: {param} {At} {node.Pos}";
                field = null;
                return false;
            }
            field = new Field(symbol);
            return Success(true, out error);
        }
        
        private static Operation GetMathFunction(QueryNode node, Context cx, out string error) {
            string      symbol  = node.ValueStr;
            Operation   operand;
            switch (symbol) {
                case "Abs":     operand = Number(node, cx, out error);  return new Abs      (operand);
                case "Ceiling": operand = Number(node, cx, out error);  return new Ceiling  (operand);
                case "Floor":   operand = Number(node, cx, out error);  return new Floor    (operand);
                case "Exp":     operand = Number(node, cx, out error);  return new Exp      (operand);
                case "Log":     operand = Number(node, cx, out error);  return new Log      (operand);
                case "Sqrt":    operand = Number(node, cx, out error);  return new Sqrt     (operand);
                default:
                    error = $"unknown function: {symbol}() {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Operation Number(QueryNode node, Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"expect field or numeric operand in {node.Label} {At} {node.Pos}";
                return null;
            }
            var operand = node.GetOperand(0);
            if (!GetOp(operand, cx, out var op, out error))
                return null;
            if (op.IsNumeric() || op is Field)
                return Success(op, out error);
            error = $"{node.Label} expect field or numeric operand. was: {operand.Label} {At} {operand.Pos}";
            return null;
        }
        
        private static Operation GetFunction(QueryNode node, Context cx, out string error) {
            var symbol  = node.ValueStr;
            var lastDot = symbol.LastIndexOf('.');
            if (lastDot == -1) {
                return GetMathFunction(node, cx, out error);
            }
            string method       = symbol.Substring(lastDot + 1);
            string fieldName    = symbol.Substring(0, lastDot);
            if (!CreateField(fieldName, node, cx, out var field, out error))
                return null;
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
                case "Count":
                    if (node.OperandCount == 0)                             return new Count(field);
                                q = Quantify(field, node, cx, out error);   return new CountWhere(q.field, q.arg, q.filter);
                
                // --- string operations
                case "Contains":    b = StringOp(field, node, cx, out error); return new Contains  (b.left, b.right);
                case "StartsWith":  b = StringOp(field, node, cx, out error); return new StartsWith(b.left, b.right);
                case "EndsWith":    b = StringOp(field, node, cx, out error); return new EndsWith  (b.left, b.right);
                case "Length":                                                return new Length    (field);
                default:
                    error = $"unknown method: {method}() used by {fieldName} {At} {node.Pos}";
                    return null;
            }
        }
        
        private static Aggregate Aggregate(Field field, QueryNode node, Context cx, out string error) {
            if (!GetArrowBody(node, 1, out QueryNode bodyNode, out error))
                return default;
            var argOperand  = node.GetOperand(0);
            if (!cx.AddParameter(argOperand, out error))
                return default;
            if (!GetOp(bodyNode, cx, out var bodyOp, out error))
                return default;
            var arg         = argOperand.ValueStr;
            return Success(new Aggregate(field, arg, bodyOp), out error);
        }
        
        private static Quantify Quantify(Field field, QueryNode node, Context cx, out string error) {
            if (!GetArrowBody(node, 1, out QueryNode bodyNode, out error))
                return default;
            var argOperand  = node.GetOperand(0);
            if (!cx.AddParameter(argOperand, out error))
                return default;
            if (!GetOp(bodyNode, cx, out var fcn, out error))
                return default;
            if (fcn is FilterOperation filter) {
                var arg = argOperand.ValueStr;
                return Success(new Quantify(field, arg, filter), out error);
            }
            error = $"quantify operation {node.Label} expect boolean lambda body. Was: {fcn} {At} {bodyNode.Pos}";
            return default;
        }
        
        private static BinaryOperands StringOp(Field field, QueryNode node, Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"expect one operand in {node.Label} {At} {node.Pos}";
                return default;
            }
            var fcnOperand  = node.GetOperand(0);
            if (!GetOp(fcnOperand, cx, out var fcn, out error))
                return default;
            if (fcn is StringLiteral || fcn is Field)
                return Success(new BinaryOperands(field, fcn), out error);
            error = $"expect string or field operand in {node.Label}. was: {fcnOperand} {At} {fcnOperand.Pos}";
            return default;
        }
        
        private static Not NotOp(QueryNode node, Context cx, out string error) {
            if (node.OperandCount != 1) {
                error = $"not operator expect one operand {At} {node.Pos}";
                return default;
            }
            var operand_0  = node.GetOperand(0);
            if (!GetOp(operand_0, cx, out var operand, out error))
                return null;
            if (operand is FilterOperation filterOperand) {
                return new Not(filterOperand);
            }
            error = $"not operator ! must use a boolean operand. Was: {operand_0.Label} {At} {operand_0.Pos}";
            return default;
        }
        
        private static void Nullary(QueryNode node, out string error) {
            if (node.OperandCount == 0) {
                error = null;
                return;
            }
            var first = node.GetOperand(0);
            error = $"unexpected operand {first.Label} on {node.Label} {At} {first.Pos}";
        }

        private static BinaryOperands Bin(QueryNode node, Context cx, OperandType type, out string error) {
            if (node.OperandCount != 2) {
                error = $"operator {node.Label} expect two operands {At} {node.Pos}";
                return default;
            }
            var operand_0 = node.GetOperand(0);
            var operand_1 = node.GetOperand(1);
            if (!GetOp(operand_0, cx, out var left, out error))
                return default;
            if (!GetOp(operand_1, cx, out var right, out error))
                return default;
            if (type == Num) {
                if ((left.IsNumeric() || left is Field) && (right.IsNumeric() || right is Field))
                    return new BinaryOperands (left, right); 
                error = $"operator {node.Label} must use numeric operands {At} {node.Pos}";
                return default;
            }
            if ((type & Bool) == 0) {
                if (left is FilterOperation || right is FilterOperation) {
                    error = $"operator {node.Label} must not use boolean operands {At} {node.Pos}";
                    return default;
                }
            }
            return new BinaryOperands (left, right);
        }
        
        private static List<FilterOperation> FilterOps(QueryNode node, Context cx, out string error) {
            if (node.OperandCount < 2) {
                error = $"expect at minimum two operands for operator {node.Label} {At} {node.Pos}";
                return null;
            }
            var operands = new List<FilterOperation> (node.OperandCount);
            for (int n = 0; n < node.OperandCount; n++) {
                var operand     = node.GetOperand(n);
                if (!GetOp(operand, cx, out var operation, out error))
                    return null;
                if (operation is FilterOperation filterOperation) {
                    operands.Add(filterOperation);
                } else {
                    error = $"operator {node.Label} expect boolean operands. Got: {operation} {At} {operand.Pos}";
                    return null;
                }
            }
            return Success(operands, out error);
        }
        
        private static T Success<T> (T result, out string error) {
            error = null;
            return result;
        }

        internal const string At = "at pos";
    }
}
