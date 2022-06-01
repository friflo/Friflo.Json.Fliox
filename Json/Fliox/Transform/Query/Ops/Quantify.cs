// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Ignore = Friflo.Json.Fliox.IgnoreFieldAttribute;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public abstract class BinaryQuantifyOp : FilterOperation
    {
        [Required]  public  Field           field;
        [Required]  public  string          arg;
        [Required]  public  FilterOperation predicate;  // e.g.   i => i.amount < 1

        protected BinaryQuantifyOp() { }
        protected BinaryQuantifyOp(Field field, string arg, FilterOperation predicate) {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.variables.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }
    }
    
    public sealed class Any : BinaryQuantifyOp
    {
        public   override string    OperationName => "Any";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Any", field, arg, predicate, cx);

        public Any() { }
        public Any(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    var result = val.EqualsTo(True, this);
                    if (result.IsError)
                        return new EvalResult(result);
                    if (result.IsTrue)
                        return SingleTrue;
                }
            }
            return SingleFalse;
        }
    }
    
    public sealed class All : BinaryQuantifyOp
    {
        public   override string    OperationName => "All";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("All", field, arg, predicate, cx);

        public All() { }
        public All(Field field, string arg, FilterOperation predicate) : base(field, arg, predicate) { }

        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    var result = val.EqualsTo(True, this);
                    if (result.IsError)
                        return new EvalResult(result);
                    if (!result.IsTrue)
                        return SingleFalse;
                }
            }
            return SingleTrue;
        }
    }
    
    public sealed class CountWhere : Operation // Note: must not extend: FilterOperation
    {
        [Required]  public          Field           field;
        [Required]  public          string          arg;
        [Required]  public          FilterOperation predicate;  // e.g.   i => i.amount < 1
        
        // is set always to the same value in Eval() so it can be reused
        [Ignore]private readonly    EvalResult      evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        public CountWhere() { }
        public CountWhere(Field field, string arg, FilterOperation predicate)  {
            this.field      = field;
            this.predicate  = predicate;
            this.arg        = arg;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            cx.variables.Add(arg, field);
            field.Init(cx, InitFlags.ArrayField);
            predicate.Init(cx, 0);
        }

        public   override string    OperationName => "Count";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqArrow("Count", field, arg, predicate, cx);

        internal override EvalResult Eval(EvalCx cx) {
            var groupEval = field.Eval(cx);
            int count = 0;
            for (int groupIndex = 0; groupIndex < groupEval.Count; groupIndex++) {
                var groupCx = new EvalCx(groupIndex);
                var eval = predicate.Eval(groupCx);
                foreach (var val in eval.values) {
                    var result = val.EqualsTo(True, this);
                    if (result.IsError)
                        return new EvalResult(result);
                    if (result.IsTrue) {
                        count++;                        
                    }
                }
            }
            evalResult.SetSingle(new Scalar(count));
            return evalResult;
        }
    }
}