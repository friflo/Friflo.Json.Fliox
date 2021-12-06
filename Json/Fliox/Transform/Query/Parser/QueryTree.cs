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
                    if (node.operation.type == token.type) {
                        pos++;
                        error = null;
                        return;
                    }
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
            AppendLabel(sb);
            return sb.ToString();
        }
        
        private void AppendLabel (StringBuilder sb) {
            sb.Append(operation.ToString());
            var shape = Token.Shape(operation.type);
            if (shape.arity == Arity.Unary)
                return;
            sb.Append("(");
            if (operands.Count > 0) {
                operands[0].AppendLabel(sb);
            }
            for (int n = 1; n < operands.Count; n++) {
                sb.Append(", ");
                operands[n].AppendLabel(sb);
            }
            sb.Append(")");
        }

        internal QueryNode (Token operation) {
            this.operation = operation;
            operands = new List<QueryNode>();
        }
    } 
}