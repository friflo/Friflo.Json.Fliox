using System.Collections.Generic;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    internal static class QueryTree
    {
        internal static Node CreateTree(Token[] tokens, out string error) {
            int     pos = 0;
            Node    node = new Node(default);
            error = null;
            while (pos < tokens.Length) {
                node  = GetNode(node, tokens, ref pos, out error);
            }
            return node;
        }

        private static Node GetNode(in Node node, Token[] tokens, ref int pos, out string error) {
            var token = tokens[pos];
            switch (token.type) {
                // --- binary tokens
                case TokenType.Add:
                case TokenType.Sub:
                case TokenType.Mul:
                case TokenType.Div:       
                //
                case TokenType.Greater:         
                case TokenType.GreaterOrEqual:
                case TokenType.Less:  
                case TokenType.LessOrEqual:
                case TokenType.Equals:
                case TokenType.NotEquals:
                    return GetRight(node, tokens, ref pos, out error);
            
                // --- unary tokens
                case TokenType.String:  pos++; error = null; return new Node(token);
                case TokenType.Double:  pos++; error = null; return new Node(token);
                case TokenType.Long:    pos++; error = null; return new Node(token);
            
                case TokenType.Symbol:  pos++; error = null; return new Node(token);
            }
            error = "ERROR";
            return null;
        }
        
        private static Node GetRight(in Node node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new Node(token);
            newNode.operands.Add(node);
            return newNode;
        }

        private static Node Error (string message, out string error) {
            error = message;
            return default;
        }
    }
    
    internal class Node {
        internal            Token       operation;
        internal readonly   List<Node>  operands;
        
        internal            int         Count           => operands.Count;        
        internal            Node        this[int index] => operands[index];
        public   override   string      ToString()      => operation.ToString();

        internal Node (Token operation) {
            this.operation = operation;
            operands = new List<Node>();
        }
    } 
}