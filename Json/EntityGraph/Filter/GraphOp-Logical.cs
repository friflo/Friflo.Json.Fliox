// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    public abstract class BoolOp : GraphOp { }
    
    // -------------------------------------- logical operators --------------------------------------
    // --- unary logical operators
    public abstract class UnaryBoolOp : BoolOp
    {
        protected BoolOp       operand;     // e.g.   i => i.amount < 1

        protected UnaryBoolOp(BoolOp operand) { this.operand = operand; }
        
        internal override void Init(GraphOpContext cx) {
            operand.Init(cx);
        }
    }
    
    public class Not : UnaryBoolOp
    {
        public override     string      ToString() => $"!({operand})";
        
        public Not(BoolOp operand) : base(operand) { }
        
        internal override List<SelectorValue> Eval() {
            var eval = operand.Eval();
            foreach (var result in eval) {
                if (result.CompareTo(True) == 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
    
    public class Any : UnaryBoolOp
    {
        public override     string      ToString() => $"Any({operand})";
        
        public Any(BoolOp operand) : base(operand) { }
        
        internal override List<SelectorValue> Eval() {
            var eval = operand.Eval();
            foreach (var result in eval) {
                if (result.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class All : UnaryBoolOp
    {
        public override     string      ToString() => $"All({operand})";
        
        public All(BoolOp operand) : base(operand) { }
        
        internal override List<SelectorValue> Eval() {
            var eval = operand.Eval();
            foreach (var result in eval) {
                if (result.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
    
    // --- (n-ary) group logical operators
    public abstract class GroupBoolOp : BoolOp
    {
        protected List<BoolOp>       operands;

        protected GroupBoolOp(List<BoolOp> operands) { this.operands = operands; }
        
        internal override void Init(GraphOpContext cx) {
            foreach (var operand in operands) {
                operand.Init(cx);
            }
        }
    }
    
    public class And : GroupBoolOp
    {
        public override     string      ToString() => string.Join(" && ", operands);
        
        public And(List<BoolOp> operands) : base(operands) { }
        
        internal override List<SelectorValue> Eval() {
            var results = new List<SelectorValue>();
            foreach (var operand in operands) {
                var eval = operand.Eval();
                results.AddRange(eval);
            }
            foreach (var result in results) {
                if (result.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
    
    public class Or : GroupBoolOp
    {
        public override     string      ToString() => string.Join(" && ", operands);
        
        public Or(List<BoolOp> operands) : base(operands) { }
        
        internal override List<SelectorValue> Eval() {
            var results = new List<SelectorValue>();
            foreach (var operand in operands) {
                var eval = operand.Eval();
                results.AddRange(eval);
            }
            foreach (var result in results) {
                if (result.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
}