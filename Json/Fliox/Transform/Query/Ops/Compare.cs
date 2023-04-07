// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Transform.Query.Arity;

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
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            left.Init(cx, 0);
            right.Init(cx, 0);
        }
    }
    
    // --- associative comparison operations ---
    public sealed class Equal : BinaryBoolOp
    {
        public Equal() { }
        public Equal(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "==";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "==", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.EqualsTo(pair.right, this);
                if (result.IsError)
                    return evalResult.SetError(result);
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class NotEqual : BinaryBoolOp
    {
        public NotEqual() { }
        public NotEqual(Operation left, Operation right) : base(left, right) { }

        public   override string    OperationName => "!=";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "!=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var result = pair.left.EqualsTo(pair.right, this);
                if (result.IsError)
                    return evalResult.SetError(result);
                if (result.IsNull)
                    evalResult.Add(Null);
                else
                    evalResult.Add(result.IsTrue ? False : True);
            }
            return evalResult;
        }
    }

    // --- non-associative comparison operations -> call Order() --- 
    public sealed class Less : BinaryBoolOp
    {
        public Less() { }
        public Less(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName => "<";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var compare = pair.left.CompareTo(pair.right, this, out Scalar result);
                if (result.IsError)
                    return evalResult.SetError(result);
                result = result.IsNull ? Null : compare < 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class LessOrEqual : BinaryBoolOp
    {
        public LessOrEqual() { }
        public LessOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName => "<=";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, "<=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var compare = pair.left.CompareTo(pair.right, this, out Scalar result);
                if (result.IsError)
                    return evalResult.SetError(result);
                result = result.IsNull ? Null : compare <= 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class Greater : BinaryBoolOp
    {
        public Greater() { }
        public Greater(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName => ">";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var compare = pair.left.CompareTo(pair.right, this, out Scalar result);
                if (result.IsError)
                    return evalResult.SetError(result);
                result = result.IsNull ? Null : compare > 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
    
    public sealed class GreaterOrEqual : BinaryBoolOp
    {
        public GreaterOrEqual() { }
        public GreaterOrEqual(Operation left, Operation right) : base(left, right) { }
        
        public   override string    OperationName => ">=";
        public   override void      AppendLinq(AppendCx cx) => AppendLinqBinary(cx, ">=", left, right);
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var compare = pair.left.CompareTo(pair.right, this, out Scalar result);
                if (result.IsError)
                    return evalResult.SetError(result);
                result = result.IsNull ? Null : compare >= 0 ? True : False;
                evalResult.Add(result);
            }
            return evalResult;
        }
    }
}