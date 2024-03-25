// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ----------------------------------- unary logical operations -----------------------------------
    public abstract class UnaryLogicalOp : FilterOperation
    {
        [Required]  public  FilterOperation     operand;     // e.g.   i => i.amount < 1

        protected UnaryLogicalOp() { }
        protected UnaryLogicalOp(FilterOperation operand) { this.operand = operand; }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx);
        }
    }
    
    public sealed class Not : UnaryLogicalOp
    {
        public   override string    OperationName           => "!";
        public   override OpType    Type                    => OpType.NOT;
        internal override void      AppendLinq(AppendCx cx) { cx.Append("!("); operand.AppendLinq(cx); cx.Append(")"); }

        public Not() { }
        public Not(FilterOperation operand) : base(operand) { }
        
        internal override Scalar Eval(EvalCx cx) {
            var val     = operand.Eval(cx);
            var isTrue  = val.EqualsTo(True, this);
            if (isTrue.IsError) {
                return isTrue;
            }
            return isTrue.IsTrue ? False : True;
        }
    }
    

    
    // ----------------------------------- (n-ary) logical group operations -----------------------------------
    public abstract class BinaryLogicalOp : FilterOperation
    {
        [Required]  public              List<FilterOperation>   operands;

        protected BinaryLogicalOp() { }
        protected BinaryLogicalOp(List<FilterOperation> operands) { this.operands = operands; }
        
        internal override void Init(OperationContext cx) {
            cx.ValidateReuse(this); // results are reused
            foreach (var operand in operands) {
                operand.Init(cx);
            }
        }
    }
    
    public sealed class And : BinaryLogicalOp
    {
        public   override string    OperationName           => "&&";
        public   override OpType    Type                    => OpType.AND;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqNAry(cx, "&&", operands);

        public And() { }
        public And(List<FilterOperation> operands) : base(operands) { }
        
        internal override Scalar Eval(EvalCx cx) {
            foreach (var operand in operands) {
                var eval    = operand.Eval(cx);
                var isTrue  = eval.EqualsTo(True, this);
                if (isTrue.IsError) {
                    return isTrue;
                }
                if (isTrue.IsFalse) {
                    return False;
                }
            }
            return True;
        }
    }
    
    public sealed class Or : BinaryLogicalOp
    {
        public   override string    OperationName           => "||";
        public   override OpType    Type                    => OpType.OR;
        internal override void      AppendLinq(AppendCx cx) => AppendLinqNAry(cx, "||", operands);
        
        public Or() { }
        public Or(List<FilterOperation> operands) : base(operands) { }
        
        internal override Scalar Eval(EvalCx cx) {
            foreach (var operand in operands) {
                var eval    = operand.Eval(cx);
                var isTrue  = eval.EqualsTo(True, this);
                if (isTrue.IsError) {
                    return isTrue;
                }
                if (isTrue.IsTrue) {
                    return True;
                }
            }
            return False;
        }
    }
}