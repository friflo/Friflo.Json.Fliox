// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public abstract class BinaryQuantifyOp : FilterOperation
    {
        [Fri.Required]  public  Field           field;
        [Fri.Required]  public  string          arg;
        [Fri.Required]  public  FilterOperation predicate;  // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp() { }
        protected BinaryQuantifyOp(Field field, string arg, FilterOperation predicate) {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.lambdaArgs.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }
    }
    
    public class Any : BinaryQuantifyOp
    {
        public override string      Linq => $"{field.Linq}.Any({arg} => {predicate.Linq})";

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
        public override string      Linq => $"{field.Linq}.All({arg} => {predicate.Linq})";
        
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
    
    public class CountWhere : Operation // Note: must not extend: FilterOperation
    {
        [Fri.Required]  public  Field           field;
        [Fri.Required]  public  string          arg;
        [Fri.Required]  public  FilterOperation predicate;  // e.g.   i => i.amount < 1
        
        // is set always to the same value in Eval() so it can be reused
        [Fri.Ignore]
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        public CountWhere() { }
        public CountWhere(Field field, string arg, FilterOperation predicate)  {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.lambdaArgs.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }

        public override string      Linq => $"{field.Linq}.Count({arg} => {predicate.Linq})";
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            int count = 0;
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    if (val.CompareTo(True) == 0) {
                        count++;                        
                    }
                }
            }
            evalResult.SetSingle(new Scalar(count));
            return evalResult;
        }
    }
}