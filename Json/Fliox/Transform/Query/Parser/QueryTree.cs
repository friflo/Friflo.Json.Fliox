using System.Collections.Generic;
using System.Text;

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
            switch (token.type) {
                // --- unary tokens
                case TokenType.String:  
                case TokenType.Double:
                case TokenType.Long:
                case TokenType.Symbol:
                    AddUnary(ref node, tokens, ref pos, out error);
                    return;
                
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
        
        private static void AddArity(ref QueryNode node, Token[] tokens, ref int pos, out string error) {
            // if (node.Count == 0)
            //    return Error("missing left operand for +", out error);
            var token = tokens[pos];
            pos++;
            error = null;
            var newNode = new QueryNode(token);
            newNode.operands.Add(node);
            node = newNode;
        }
    }
    
    // todo - check: change to struct
    internal class QueryNode {
        internal            Token           operation;
        internal readonly   List<QueryNode> operands;
        
        internal            int             Count           => operands.Count;        
        internal            QueryNode       this[int index] => operands[index];
        
        public   override   string          ToString() {
            var sb = new StringBuilder();
            sb.Append(operation.ToString());
            sb.Append(" (");
            if (operands.Count > 0)
                sb.Append(operands[0].operation.ToString());
            for (int n = 1; n < operands.Count; n++) {
                sb.Append(", ");
                sb.Append(operands[n].operation.ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }

        internal QueryNode (Token operation) {
            this.operation = operation;
            operands = new List<QueryNode>();
        }
    } 
}