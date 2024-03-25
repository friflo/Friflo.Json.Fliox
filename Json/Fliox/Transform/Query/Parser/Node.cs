// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    public sealed class QueryNode {
        private  readonly   Token           operation;
        internal readonly   int             precedence;
        internal            Arity           arity;
        internal            bool            bracketClosed;
        
        internal            string          Label       => operation.GetLabel(true);
        internal            TokenType       TokenType   => operation.type;
        internal            string          ValueStr    => operation.str;
        internal            double          ValueDbl    => operation.dbl;
        internal            long            ValueLng    => operation.lng;
        internal            int             Pos         => operation.pos;
        
        private  readonly   List<QueryNode> operands;   // intentionally private. optimize: could avoid List<> in most cases 
        
        internal            int             OperandCount                                => operands.Count;        
        internal            QueryNode       GetOperand(int index)                       => operands[index];
        internal            void            SetOperand(int index, QueryNode operand)    => operands[index] = operand;
        internal            void            AddOperand(QueryNode operand) {
            if (operand == null) throw new ArgumentNullException(nameof(operand));
            operands.Add(operand);
        }

        public   override   string          ToString() {
            var sb = new StringBuilder();
            AppendLabel(sb);
            return sb.ToString();
        }

        private void AppendLabel (StringBuilder sb) {
            sb.Append(operation);
            var operandsCount = operands.Count;
            if (operandsCount == 0)
                return;
            sb.Append(" {");
            if (operandsCount > 0) {
                operands[0].AppendLabel(sb);
            }
            for (int n = 1; n < operandsCount; n++) {
                sb.Append(", ");
                operands[n].AppendLabel(sb);
            }
            sb.Append('}');
        }
        
        internal QueryNode (in Token operation) {
            AssertValidTokenType(operation.type);
            this.operation  = operation;
            operands        = new List<QueryNode>();
            var shape       = Token.Shape(operation.type);
            arity           = shape.arity;
            precedence      = shape.precedence;
        }
        
        [Conditional("DEBUG")]
        private static void AssertValidTokenType(TokenType type) {
           switch (type) {
               case TokenType.Start:
               case TokenType.End:
               case TokenType.Whitespace:
               case TokenType.Error:
               case TokenType.BracketClose:
                   throw new InvalidOperationException($"Must not create a QueryNode with operation type: {type}");
           }
        }
    } 
}