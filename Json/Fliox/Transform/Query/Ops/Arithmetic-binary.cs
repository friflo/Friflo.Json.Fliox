// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Transform.Query.Arity;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------ binary arithmetic operations ------------------------------------
    public abstract class BinaryArithmeticOp : Operation
    {
        [Required]  public  Operation   left;
        [Required]  public  Operation   right;
        
        internal readonly   EvalResult  evalResult = new EvalResult(new List<Scalar>());
        internal override   bool        IsNumeric => true;

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

        public   override string    OperationName => "+";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "+", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Add(pair.right, this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Subtract : BinaryArithmeticOp
    {
        public Subtract() { }
        public Subtract(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "-";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "-", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Subtract(pair.right, this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Multiply : BinaryArithmeticOp
    {
        public Multiply() { }
        public Multiply(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "*";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "*", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Multiply(pair.right, this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Divide : BinaryArithmeticOp
    {
        public Divide() { }
        public Divide(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "/";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "/", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Divide(pair.right, this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Modulo : BinaryArithmeticOp
    {
        public Modulo() { }
        public Modulo(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "%";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "%", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.Modulo(pair.right, this);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}
