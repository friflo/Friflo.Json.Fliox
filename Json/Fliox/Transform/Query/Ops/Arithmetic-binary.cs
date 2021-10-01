// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------ binary arithmetic operations ------------------------------------
    public abstract class BinaryArithmeticOp : Operation
    {
        [Fri.Required]  public  Operation   left;
        [Fri.Required]  public  Operation   right;
        internal readonly       EvalResult  evalResult = new EvalResult(new List<Scalar>());

        protected BinaryArithmeticOp() { }
        protected BinaryArithmeticOp(Operation left, Operation right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx, 0);
            right.Init(cx, 0);
        }
    }
    
    public sealed class Add : BinaryArithmeticOp
    {
        public Add() { }
        public Add(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} + {right.Linq}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Add(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Subtract : BinaryArithmeticOp
    {
        public Subtract() { }
        public Subtract(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} - {right.Linq}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Subtract(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Multiply : BinaryArithmeticOp
    {
        public Multiply() { }
        public Multiply(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} * {right.Linq}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Multiply(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Divide : BinaryArithmeticOp
    {
        public Divide() { }
        public Divide(Operation left, Operation right) : base(left, right) { }

        public override string      Linq => $"{left.Linq} / {right.Linq}";
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Divide(pair.right);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}
