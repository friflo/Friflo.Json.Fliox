// Copyright (c) Ullrich Praetz. All rights reserved.
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
    public static class QueryTree
    {
        public static QueryNode CreateTree (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            if (error != null)
                return null;
            var node    = CreateTree(result.items,out error);
            return node;
        }

        internal static QueryNode CreateTree(Token[] tokens, out string error) {
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
                if (entry.operation.type == TokenType.BracketOpen && !entry.bracketClosed) {
                    error = "missing closing parenthesis";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static void GetNode(Stack<QueryNode> stack, in Token token, out string error) {
            var shape = Token.Shape(token.type);
            switch (shape.arity) {
                case Arity.Unary:   AddUnary    (stack, token, out error);  return;
                case Arity.Binary:  AddBinary   (stack, token, out error);  return;
                case Arity.NAry:    AddNAry     (stack, token, out error);  return;
                default:
                    switch (token.type){
                        case TokenType.BracketOpen:     HandleBracketOpen(stack, out error);    return;
                        case TokenType.BracketClose:    HandleBracketClose(stack, out error);   return;
                        default:
                            error = $"Unexpected query token: {token}";
                            return;
                    }
            }
        }

        private static void HandleBracketOpen(Stack<QueryNode> stack, out string error) {
            stack.TryPeek(out QueryNode last);

            // add (grouping) open parenthesis
            var newNode = new QueryNode(new Token(TokenType.BracketOpen));
            last?.AddOperand(newNode);
            stack.Push(newNode);
            error = null;
        }
        
        private static void HandleBracketClose(Stack<QueryNode> stack, out string error) {
            while (true) {
                if (!stack.TryPeek(out QueryNode head)) {
                    error = "no matching open parenthesis";
                    return;
                }
                if (head.isFunction) {
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
                if (head.operation.type == TokenType.BracketOpen) {
                    // found matching (grouping) open parenthesis
                    head.bracketClosed = true;
                    error = null;
                    return;
                }
                stack.Pop();
            }
        }
        
        private static void AddUnary(Stack<QueryNode> stack, in Token token, out string error) {
            error = null;
            var newNode = new QueryNode(token);
            if (token.type == TokenType.Function) {
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
            if (token.type == TokenType.Function) {
                stack.Push(newNode);
            }
        }

        private static void AddBinary(Stack<QueryNode> stack, in Token token, out string error) {
            if (!stack.TryPeek(out QueryNode head)) {
                error = $"operator {token} expect one preceding operand";
                return;
            }
            if (token.type == TokenType.Arrow) {
                HandleArrow(head, out error);
                return;
            }
            error           = null;
            var newNode     = new QueryNode(token);
            PushNode(stack, newNode);
        }
        
        private static void HandleArrow(QueryNode head, out string error) {
            if (!head.isFunction) {
                error = $"=> can be used only as lambda in functions. Was used by: {head.operation}";
                return;
            }
            if (head.OperandCount != 1) {
                error = $"=> expect one preceding lambda argument. Was used in: {head.operation}";
                return;
            }
            var lambdaArg = head.GetOperand(0);
            if (lambdaArg.operation.type != TokenType.Symbol) {
                error = $"=> lambda argument must by a symbol name. Was: {lambdaArg.operation} in {head.operation}";
                return;
            }
            error = null;
            // success
            // note: arrow operands are added to parent function. So the => itself is not added as a new node. 
        }
        
        /// <summary>
        /// Push <see cref="newNode"/> to the stack.
        /// In case <see cref="newNode"/> is not unary it:
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
                error = $"operator {token} expect one preceding operand";
                return;
            }
            error = null;
            if (node.operation.type == token.type) {
                // || &&  are allowed having multiple operators 
                return;
            }
            var newNode = new QueryNode(token);
            PushNode(stack, newNode);
        }
    }
}