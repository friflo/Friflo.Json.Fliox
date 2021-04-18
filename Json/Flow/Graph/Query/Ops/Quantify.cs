// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Graph.Query.Ops
{
    public abstract class BinaryQuantifyOp : FilterOperation
    {
        public      Field           field;
        public      string          arg;
        public      FilterOperation predicate;  // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp() { }
        protected BinaryQuantifyOp(Field field, string arg, FilterOperation predicate) {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.parameters.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }
    }
    
    public class Any : BinaryQuantifyOp
    {
        public override     string      ToString() => $"{field}.Any({arg} => {predicate})";

        public Any() { }
        public Any(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }
        
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
        public override     string      ToString() => $"{field}.All({arg} => {predicate})";
        
        public All() { }
        public All(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }
        
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