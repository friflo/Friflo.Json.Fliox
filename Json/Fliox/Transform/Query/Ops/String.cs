// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Contains : BinaryBoolOp
    {
        public   override string    OperationName => "Contains";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqMethod("Contains", left, right, cx);

        public Contains() { }
        public Contains(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var contains = pair.left.Contains(pair.right, this);
                evalResult.Add(contains);
            }
            return evalResult;
        }
    }
    
    public sealed class StartsWith : BinaryBoolOp
    {
        public   override string    OperationName => "StartsWith";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqMethod("StartsWith", left, right, cx);

        public StartsWith() { }
        public StartsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var startsWith = pair.left.StartsWith(pair.right, this);
                evalResult.Add(startsWith);
            }
            return evalResult;
        }
    }
    
    public sealed class EndsWith : BinaryBoolOp
    {
        public   override string    OperationName => "EndsWith";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqMethod("EndsWith", left, right, cx);

        public EndsWith() { }
        public EndsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var endsWith = pair.left.EndsWith(pair.right, this);
                evalResult.Add(endsWith);
            }
            return evalResult;
        }
    }
    
    public sealed class Length : Operation
    {
        [Required]  public              Operation   value;
        [Ignore]    private  readonly   EvalResult  evalResult = new EvalResult(new List<Scalar>());
                    internal override   bool        IsNumeric => true;
        
                    public   override   string      OperationName => "Length";
                    public   override   void        AppendLinq(AppendCx cx) => AppendLinqMethod("Length", value, cx);

        /// Could Extend <see cref="UnaryArithmeticOp"/> but Length() is not an arithmetic function  
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            value.Init(cx, 0);
        }

        public Length() { }
        public Length(Operation value) {
            this.value = value;
        }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = value.Eval(cx);
            foreach (var val in eval.values) {
                var result = val.Length(this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}