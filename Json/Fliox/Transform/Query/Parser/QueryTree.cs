// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

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
    internal static class QueryTree
    {
        internal static QueryNode CreateTree(Token[] tokens, out string error) {
            error       = null;
            int pos     = 0;
            var stack   = new Stack<QueryNode>();
            while (pos < tokens.Length) {
                GetNode(stack, tokens, ref pos, out error);
            }
            if (stack.Count == 0)
                throw new InvalidOperationException("invalid stack count");
            while (stack.Count > 1)
                stack.Pop();
            var result = stack.Peek();
            return result;
        }

        private static void GetNode(Stack<QueryNode> stack, Token[] tokens, ref int pos, out string error) {
            var token = tokens[pos];
            var shape = Token.Shape(token.type);
            switch (shape.arity) {
                case Arity.Unary:
                    AddUnary (stack, tokens, ref pos, out error);
                    return;
                case Arity.Binary:
                    AddBinary(stack, tokens, ref pos, out error);
                    return;
                case Arity.NAry:
                    AddNAry  (stack, tokens, ref pos, out error);
                    return;
                default:
                    error = $"Invalid arity for token: {token}";
                    return;
            }
        }
        
        private static void AddUnary(Stack<QueryNode> stack, Token[] tokens, ref int pos, out string error) {
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            if (stack.Count == 0) {
                stack.Push(newNode);
                return;                
            }
            var node = stack.Peek();
            node.operands.Add(newNode);
        }

        private static void AddBinary(Stack<QueryNode> stack, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            AddNode(stack, newNode);
        }

        private static void AddNode(Stack<QueryNode> stack, QueryNode newNode) {
            var node = stack.Peek();
            var token = newNode.operation;
            var current  = Token.Shape(token.type);
            if (current.arity != Arity.Unary) {
                var previous = Token.Shape(node.operation.type);
                if (previous.arity != Arity.Unary) {
                    if (current.precedence < previous.precedence) {
                        newNode.operands.Add(node.operands[1]);
                        node.operands[1] = newNode;
                        stack.Push(newNode);
                        return;
                    }
                }
            }
            stack.Pop();
            newNode.operands.Add(node);
            stack.Push(newNode);
        }

        private static void AddNAry(Stack<QueryNode> stack, Token[] tokens, ref int pos, out string error) {
            var node = stack.Peek();
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            if (node.operation.type == token.type) {
                // || &&  are allowed having multiple operators 
                pos++;
                error = null;
                return;
            }
            stack.Pop();
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            newNode.operands.Add(node);
            stack.Push(newNode);
        }
    }
}