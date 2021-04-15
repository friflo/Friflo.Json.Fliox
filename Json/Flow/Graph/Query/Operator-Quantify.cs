// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Graph.Query
{
    public abstract class BinaryQuantifyOp : BoolOp
    {
        protected       Field       field;
        protected       BoolOp      predicate;     // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp(Field field, BoolOp predicate) {
            this.field      = field;
            this.predicate  = predicate;
        }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            field.Init(cx);
            predicate.Init(cx);
        }
    }
    
    public class Any : BinaryQuantifyOp
    {
        public override     string      ToString() => $"Any({field}, {predicate})";

        public Any(Field field, BoolOp predicate) : base(field, predicate) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    if (val.CompareTo(True) == 0)
                        return SingleTrue;
                }
            }
            return SingleFalse;
        }
    }
    
    public class All : BinaryQuantifyOp
    {
        public override     string      ToString() => $"All({field}, {predicate})";
        
        public All(Field field, BoolOp predicate) : base(field, predicate) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    if (val.CompareTo(True) != 0)
                        return SingleFalse;
                }
            }
            return SingleTrue;
        }
    }
}