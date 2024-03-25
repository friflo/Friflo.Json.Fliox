// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // -------------------------------------- comparison operations --------------------------------------
    public abstract class BinaryBoolOp : FilterOperation
    {
        [Required]  public  Operation   left;
        [Required]  public  Operation   right;
        
        protected BinaryBoolOp() { }
        protected BinaryBoolOp(Operation left, Operation right) {
            this.left = left;
            this.right = right;
        }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx);
            right.Init(cx);
        }
    }
    
    // --- associative comparison operations ---
    public sealed class Equal : BinaryBoolOp
    {
        public Equal() { }
        public Equal(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "==";
        public   override OpType    Type                    => OpType.EQUAL;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "==", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            return leftValue.EqualsTo(rightValue, this);
        }
    }
    
    public sealed class NotEqual : BinaryBoolOp
    {
        public NotEqual() { }
        public NotEqual(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName           => "!=";
        public   override OpType    Type                    => OpType.NOT_EQUAL;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "!=", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            var result = leftValue.EqualsTo(rightValue, this);
            if (result.IsError)
                return result;
            if (result.IsNull)
                return Null;
            return result.IsTrue ? False : True;
        }
    }

    // --- non-associative comparison operations -> call Order() --- 
    public sealed class Less : BinaryBoolOp
    {
        public Less() { }
        public Less(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName           => "<";
        public   override OpType    Type                    => OpType.LESS;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            var compare     = leftValue.CompareTo(rightValue, this, out Scalar result);
            if (result.IsError)
                return result;
            return result.IsNull ? Null : compare < 0 ? True : False;
        }
    }
    
    public sealed class LessOrEqual : BinaryBoolOp
    {
        public LessOrEqual() { }
        public LessOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName           => "<=";
        public   override OpType    Type                    => OpType.LESS_OR_EQUAL;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<=", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            var compare     = leftValue.CompareTo(rightValue, this, out Scalar result);
            if (result.IsError)
                return result;
            return result.IsNull ? Null : compare <= 0 ? True : False;
        }
    }
    
    public sealed class Greater : BinaryBoolOp
    {
        public Greater() { }
        public Greater(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName           => ">";
        public   override OpType    Type                    => OpType.GREATER;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            var compare     = leftValue.CompareTo(rightValue, this, out Scalar result);
            if (result.IsError)
                return result;
            return result.IsNull ? Null : compare > 0 ? True : False;
        }
    }
    
    public sealed class GreaterOrEqual : BinaryBoolOp
    {
        public GreaterOrEqual() { }
        public GreaterOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName           => ">=";
        public   override OpType    Type                    => OpType.GREATER_OR_EQUAL;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">=", left, right);
        
        internal override Scalar Eval(EvalCx cx) {
            var leftValue   = left.Eval(cx);
            var rightValue  = right.Eval(cx);
            var compare     = leftValue.CompareTo(rightValue, this, out Scalar result);
            if (result.IsError)
                return result;
            return result.IsNull ? Null : compare >= 0 ? True : False;
        }
    }
}