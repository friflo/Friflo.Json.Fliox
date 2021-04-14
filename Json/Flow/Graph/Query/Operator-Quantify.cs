// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Graph.Query
{
    public abstract class BinaryQuantifyOp : BoolOp
    {
        protected       Field       group;
        protected       BoolOp      operand;     // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp(Field group, BoolOp operand) {
            this.group      = group;
            this.operand    = operand;
        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            group.Init(cx);
            operand.Init(cx);
        }
    }
    
    public class Any : BinaryQuantifyOp
    {
        public override     string      ToString() => $"Any({operand})";

        public Any(Field group, BoolOp operand) : base(group, operand) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = group.Eval(cx);
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                if (val.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class All : BinaryQuantifyOp
    {
        public override     string      ToString() => $"All({operand})";
        
        public All(Field group, BoolOp operand) : base(group, operand) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = group.Eval(cx);
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                if (val.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
}