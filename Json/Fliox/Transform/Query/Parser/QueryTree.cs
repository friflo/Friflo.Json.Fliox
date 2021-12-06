// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

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
            int         pos     = 0;
            QueryNode   root    = new QueryNode(default);
            QueryNode   node = root;
            error = null;
            while (pos < tokens.Length) {
                GetNode(ref node, tokens, ref pos, out error);
            }
            return node;
        }

        private static void GetNode(ref QueryNode node, Token[] tokens, ref int pos, out string error) {
            var token = tokens[pos];
            var shape = Token.Shape(token.type);
            switch (shape.arity) {
                case Arity.Unary:
                    AddUnary (ref node, tokens, ref pos, out error);
                    return;
                case Arity.Binary:
                    AddBinary(ref node, tokens, ref pos, out error);
                    return;
                case Arity.NAry:
                    AddNAry (ref node, tokens, ref pos, out error);
                    return;
                default:
                    error = $"Invalid arity for token: {token}";
                    return;
            }
        }
        
        private static void AddUnary(ref QueryNode node, Token[] tokens, ref int pos, out string error) {
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            if (node.Count == 0) {
                node = newNode;                
            } else {
                node.operands.Add(newNode);
            }
        }

        private static void AddBinary(ref QueryNode node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            newNode.operands.Add(node);
            node = newNode;
        }
        
        private static void AddNAry(ref QueryNode node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            if (node.operation.type == token.type) {
                // || &&  are allowed having multiple operators 
                pos++;
                error = null;
                return;
            }
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            newNode.operands.Add(node);
            node = newNode;
        }
    }
}