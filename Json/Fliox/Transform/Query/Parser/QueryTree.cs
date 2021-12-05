using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    // example:  true || 1 < 2
    //
    //  1: true     2: ||       3: 1        4: <        5: 2                
    //
    //                  true       true        true        true
    //                /           /           /           /
    //  true        ||          ||          ||   1      ||  1 
    //                            \           \ /        \ /
    //                             1           <          <
    //                                                     \
    //                                                      2
    internal static class QueryTree
    {
        internal static Node CreateTree(Token[] tokens, out string error) {
            int     pos     = 0;
            Node    root    = new Node(default);
            Node    node = root;
            error = null;
            while (pos < tokens.Length) {
                GetNode(ref node, tokens, ref pos, out error);
            }
            return node;
        }

        private static void GetNode(ref Node node, Token[] tokens, ref int pos, out string error) {
            var token = tokens[pos];
            switch (token.type) {
                // --- unary tokens
                case TokenType.String:  AddUnary(ref node, tokens, ref pos, out error);     return;
                case TokenType.Double:  AddUnary(ref node, tokens, ref pos, out error);     return;
                case TokenType.Long:    AddUnary(ref node, tokens, ref pos, out error);     return;
                case TokenType.Symbol:  AddUnary(ref node, tokens, ref pos, out error);     return;
                
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
                    AddBinary(ref node, tokens, ref pos, out error);
                    return;
                
                // --- arity tokens
                case TokenType.Or:
                case TokenType.And:
                    if (node.operation.type == token.type) {
                        pos++;
                        error = null;
                        return;
                    }
                    AddArity(ref node, tokens, ref pos, out error);
                    return;
            }
            error = "ERROR";
        }
        
        private static void AddUnary(ref Node node, Token[] tokens, ref int pos, out string error) {
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new Node(token);
            if (node.Count == 0) {
                node = newNode;                
            } else {
                node.operands.Add(newNode);
            }
        }

        private static void AddBinary(ref Node node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new Node(token);
            newNode.operands.Add(node);
            node = newNode;
        }
        
        private static void AddArity(ref Node node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new Node(token);
            newNode.operands.Add(node);
            node = newNode;
        }
    }
    
    // todo - check: change to struct
    internal class Node {
        internal            Token       operation;
        internal readonly   List<Node>  operands;
        
        internal            int         Count           => operands.Count;        
        internal            Node        this[int index] => operands[index];
        public   override   string      ToString() {
            var sb = new StringBuilder();
            sb.Append(operation.ToString());
            sb.Append(" (");
            foreach (var operand in operands) {
                sb.Append(' ');
                sb.Append(operand.operation.ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }

        internal Node (Token operation) {
            this.operation = operation;
            operands = new List<Node>();
        }
    } 
}