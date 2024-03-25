// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBeProtected.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ------------------------------------ binary arithmetic operations ------------------------------------
    public abstract class BinaryArithmeticOp : Operation
    {
        [Required]  public  Operation   left;
        [Required]  public  Operation   right;
        
        internal override   bool        IsNumeric() => true;

        protected BinaryArithmeticOp() { }
        protected BinaryArithmeticOp(Operation left, Operation right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    public sealed class Add : BinaryArithmeticOp
    {
        public Add() { }
        public Add(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "+";
        public   override OpType    Type                    => OpType.ADD;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "+", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Add(rightValue, this);
        }
    }
    
    public sealed class Subtract : BinaryArithmeticOp
    {
        public Subtract() { }
        public Subtract(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "-";
        public   override OpType    Type                    => OpType.SUBTRACT;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "-", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Subtract(rightValue, this);
        }
    }
    
    public sealed class Multiply : BinaryArithmeticOp
    {
        public Multiply() { }
        public Multiply(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "*";
        public   override OpType    Type                    => OpType.MULTIPLY;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "*", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Multiply(rightValue, this);
        }
    }
    
    public sealed class Divide : BinaryArithmeticOp
    {
        public Divide() { }
        public Divide(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "/";
        public   override OpType    Type                    => OpType.DIVIDE;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "/", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Divide(rightValue, this);
        }
    }
    
    public sealed class Modulo : BinaryArithmeticOp
    {
        public Modulo() { }
        public Modulo(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "%";
        public   override OpType    Type                    => OpType.MODULO;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "%", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.Modulo(rightValue, this);
        }
    }
}
