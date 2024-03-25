// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    // example:  a * b + c   (5 tokens)
    //
    //  a (1)       * (2)       b (3)       + (4)       c (5)
    //             
    //                a           a             a           a
    //               /           /             /           /
    //  a           *           *             *           *
    //                           \           / \         / \  
    //                            b         +   b       +   b       
    //                                                   \
    //                                                    c
    // For understanding:
    // operator with lowest precedence (+) is root.
    // Sounds irritating on first look but make sense in design of expression trees.
    //
    /// <summary>
    /// <see cref="QueryParser"/> iterate the <see cref="Token"/>'s returned from <see cref="QueryLexer"/> and build
    /// a tree of nodes with the root node returned by <see cref="CreateTree"/>.
    /// </summary>
    public static class QueryParser
    {
        public static QueryNode CreateTree (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            if (error != null)
                return null;
            var node    = CreateTreeIntern(result.items,out error);
            return node;
        }

        internal static QueryNode CreateTreeIntern(Token[] tokens, out string error) {
            error       = null;
            int pos     = 0;
            var stack   = new Stack<QueryNode>();
            while (pos < tokens.Length) {
                var token = tokens[pos++];
                GetNode(stack, token, out error);
                if (error != null)
                    return null;
            }
            if (!ValidateStack(stack, out error))
                return null;
            while (stack.Count > 1)
                stack.Pop();
            var result = stack.Peek();
            return result;
        }
        
        private static bool ValidateStack(Stack<QueryNode> stack, out string error) {
            if (stack.Count == 0)
                throw new InvalidOperationException("invalid stack count");
            foreach (var entry in stack) {
                if (entry.TokenType == TokenType.BracketOpen && !entry.bracketClosed) {
                    error = $"missing closing parenthesis {At} {entry.Pos}";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static void GetNode(Stack<QueryNode> stack, in Token token, out string error) {
            var shape = Token.Shape(token.type);
            switch (shape.arity) {
                case Arity.Unary:               AddUnary    (stack, token, out error);  return;
                case Arity.Binary:              AddBinary   (stack, token, out error);  return;
                case Arity.NAry:                AddNAry     (stack, token, out error);  return;
            }
            switch (token.type){
                case TokenType.BracketOpen:     BracketOpen (stack, token, out error);  return;
                case TokenType.BracketClose:    BracketClose(stack, token, out error);  return;
                default:
                    error = $"Unexpected query token: {token} {At} {token.pos}";
                    return;
            }
        }

        private static void BracketOpen(Stack<QueryNode> stack, in Token token, out string error) {
            stack.TryPeek(out QueryNode last);

            // add (grouping) open parenthesis
            var newNode = new QueryNode(token);
            last?.AddOperand(newNode);
            stack.Push(newNode);
            error = null;
        }
        
        private static void BracketClose(Stack<QueryNode> stack, in Token token, out string error) {
            while (true) {
                if (!stack.TryPeek(out QueryNode head)) {
                    error = $"no matching open parenthesis {At} {token.pos}";
                    return;
                }
                if (head.TokenType == TokenType.Function) {
                    // A closing bracket causes the head node to be used as an Unary node.
                    // So its last operand will not be used as the left operand for subsequent n-ary operations (n>1).
                    if (stack.Count > 1) {
                        stack.Pop();
                    } else {
                        head.arity = Arity.Unary;
                    }
                    error = null;
                    return;
                }
                if (head.TokenType == TokenType.BracketOpen) {
                    // found matching (grouping) open parenthesis
                    if (stack.Count > 1) {
                        stack.Pop();
                    } else {
                        head.bracketClosed = true;
                    }
                    error = null;
                    return;
                }
                stack.Pop();
            }
        }
        
        private static void AddUnary(Stack<QueryNode> stack, in Token token, out string error) {
            error = null;
            var newNode = new QueryNode(token);
            var expectOperand = token.type == TokenType.Function || token.type == TokenType.Not;
            if (expectOperand) {
                // Function can accept 0 or 1 parameter.
                // 0: .children.Count()
                // 1: .children.Min(child => child.age)
                // => so it becomes NAry
                newNode.arity = Arity.NAry;
            }
            if (stack.Count == 0) {
                stack.Push(newNode);
                return;
            }
            var node = stack.Peek();
            node.AddOperand(newNode);
            if (expectOperand) {
                stack.Push(newNode);
            }
        }

        private static void AddBinary(Stack<QueryNode> stack, in Token token, out string error) {
            if (!stack.TryPeek(out QueryNode head)) {
                error = $"operator {token} expect one preceding operand {At} {token.pos}";
                return;
            }
            if (token.type == TokenType.Arrow) {
                HandleArrow(stack, head, token, out error);
                return;
            }
            error           = null;
            var newNode     = new QueryNode(token);
            PushNode(stack, newNode);
        }
        
        /// Arrow tokens (=>) are added as <see cref="QueryNode"/> and accessed by <see cref="QueryBuilder.GetArrowBody"/> 
        private static void HandleArrow(Stack<QueryNode> stack, QueryNode head, in Token token, out string error) {
            switch (head.TokenType) {
                case TokenType.Function:
                    if (head.OperandCount != 1) {
                        error = $"=> expect one preceding lambda argument. Used in: {head.Label} {At} {token.pos}";
                        return;
                    }
                    var lambdaArg = head.GetOperand(0);
                    if (lambdaArg.TokenType != TokenType.Symbol) {
                        error = $"=> lambda argument must be a parameter name. Was: {lambdaArg.Label} in {head.Label} {At} {lambdaArg.Pos}";
                        return;
                    }
                    break;
                case TokenType.Symbol:
                    break;
                default:
                    error = $"=> can be used only as lambda in functions. Used by: {head.Label} {At} {head.Pos}";
                    return;
            }
            var arrowNode = new QueryNode(token);
            head.AddOperand(arrowNode);
            stack.Push(arrowNode);
            error = null;
        }
        
        /// <summary>
        /// Push <paramref name="newNode"/> to the stack.
        /// In case <paramref name="newNode"/> is not unary it:
        /// <list type="bullet">
        ///   <item> either replaces the last operand of an operation with lower precedence </item>
        ///   <item> or gets the new stack root and add the old root as first operand </item>
        /// </list>
        /// </summary>
        private static void PushNode(Stack<QueryNode> stack, QueryNode newNode) {
            var node        = stack.Peek(); // calling method already checked stack not empty 
            var precedence  = newNode.precedence;
            if (newNode.arity == Arity.Unary || node.arity == Arity.Unary) {
                stack.Pop();
                newNode.AddOperand(node);
                stack.Push(newNode);
                return;
            }
            while (true) {
                if (precedence < node.precedence) {
                    // replace operand of stack.Head
                    var last = node.OperandCount - 1;
                    newNode.AddOperand(node.GetOperand(last));
                    node.SetOperand(last, newNode);
                    stack.Push(newNode);
                    return;
                }
                stack.Pop();
                if (stack.Count == 0)
                    break;
                node = stack.Peek();
            }
            newNode.AddOperand(node);
            stack.Push(newNode);
        }

        private static void AddNAry(Stack<QueryNode> stack, in Token token, out string error) {
            if (!stack.TryPeek(out QueryNode node)) {
                error = $"operator {token} expect one preceding operand {At} {token.pos}";
                return;
            }
            error = null;
            if (node.TokenType == token.type) {
                // || &&  are allowed having multiple operators 
                return;
            }
            var newNode = new QueryNode(token);
            PushNode(stack, newNode);
        }
        
        private const string At = "at pos";
    }
}