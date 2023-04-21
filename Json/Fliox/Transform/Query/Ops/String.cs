// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Contains : BinaryBoolOp
    {
        public   override string    OperationName           => "Contains";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqMethod("Contains", left, right, cx);

        public Contains() { }
        public Contains(Operation left, Operation right) : base(left, right) { }
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Contains(rightValue, this);
        }
    }
    
    public sealed class StartsWith : BinaryBoolOp
    {
        public   override string    OperationName           => "StartsWith";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqMethod("StartsWith", left, right, cx);

        public StartsWith() { }
        public StartsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.StartsWith(rightValue, this);
        }
    }
    
    public sealed class EndsWith : BinaryBoolOp
    {
        public   override string    OperationName           => "EndsWith";
        internal override void      AppendLinq(AppendCx cx) => AppendLinqMethod("EndsWith", left, right, cx);

        public EndsWith() { }
        public EndsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.EndsWith(rightValue, this);
        }
    }
    
    public sealed class Length : Operation
    {
        [Required]  public              Operation   value;
                    internal override   bool        IsNumeric()             => true;
                    public   override   string      OperationName           => "Length";
                    internal override   void        AppendLinq(AppendCx cx) => AppendLinqMethod("Length", value, cx);

        /// Could Extend <see cref="UnaryArithmeticOp"/> but Length() is not an arithmetic function  
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            value.Init(cx);
        }

        public Length() { }
        public Length(Operation value) {
            this.value = value;
        }
        
        internal override Scalar Eval(EvalCx cx) {
            var eval = value.Eval(cx);
            return eval.Length(this);
        }
    }
}