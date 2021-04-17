// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Graph.Query
{
    public abstract class BinaryQuantifyOp : BoolOp
    {
        public      Field       field;
        public      string      parameter;
        public      BoolOp      predicate;  // e.g.   i => i.amount < 1

        
        protected BinaryQuantifyOp(Field field, string parameter, BoolOp predicate) {
            this.field      = field;
            this.predicate  = predicate;
            this.parameter  = parameter;
        }
        
        internal override void Init(OperatorContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.parameters.Add(parameter, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }
    }
    
    public class Any : BinaryQuantifyOp
    {
        public override     string      ToString() => $"{field}.Any({parameter} => {predicate})";

        public Any(Field field, string parameter, BoolOp predicate) : base(field, parameter, predicate) { }
        
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
        public override     string      ToString() => $"{field}.All({parameter} => {predicate})";
        
        public All(Field field, string parameter, BoolOp predicate) : base(field, parameter, predicate) { }
        
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