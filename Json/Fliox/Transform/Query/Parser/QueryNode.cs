// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public class QueryNode {
        internal readonly   Token           operation;
        internal readonly   Arity           arity;
        internal readonly   int             precedence;
        internal            bool            isFunction;
        
        private  readonly   List<QueryNode> operands;   // intentionally private. optimize: could avoid List<> in most cases 
        
        internal            int             OperandCount                            => operands.Count;        
        internal            QueryNode       GetOperand(int index)                   => operands[index];
        internal            void            SetOperand(int index, QueryNode node)   => operands[index] = node;
        internal            void            AddOperand(QueryNode node)              => operands.Add(node);
        
        public   override   string          ToString() {
            var sb = new StringBuilder();
            AppendLabel(sb);
            return sb.ToString();
        }

        private void AppendLabel (StringBuilder sb) {
            sb.Append(operation.ToString());
            if (arity == Arity.Unary)
                return;
            sb.Append(" {");
            if (operands.Count > 0) {
                operands[0].AppendLabel(sb);
            }
            for (int n = 1; n < operands.Count; n++) {
                sb.Append(", ");
                operands[n].AppendLabel(sb);
            }
            sb.Append("}");
        }

        internal QueryNode (Token operation) {
            this.operation  = operation;
            operands        = new List<QueryNode>();
            var shape       = Token.Shape(operation.type);
            arity           = shape.arity;
            precedence      = shape.precedence;
        }
    } 
}