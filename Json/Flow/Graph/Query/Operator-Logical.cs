// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Query.Arity;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph.Query
{
    public abstract class BoolOp : Operator
    {
        internal readonly  EvalResult   evalResult = new EvalResult(new List<Scalar>());

        public JsonFilter Filter() {
            return new JsonFilter(this);
        }
    }
    
    // ----------------------------------- unary logical operators -----------------------------------
    public abstract class UnaryBoolOp : BoolOp
    {
        protected           BoolOp              operand;     // e.g.   i => i.amount < 1

        protected UnaryBoolOp(BoolOp operand) { this.operand = operand; }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx);
        }
    }
    
    public class Not : UnaryBoolOp
    {
        public override     string      ToString() => $"!({operand})";
        
        public Not(BoolOp operand) : base(operand) { }
        
        internal override EvalResult Eval() {
            evalResult.Clear();
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                evalResult.Add(val.CompareTo(True) == 0 ? False : True);
            }
            return evalResult;
        }
    }
    
    public class Any : UnaryBoolOp
    {
        public override     string      ToString() => $"Any({operand})";
        
        public Any(BoolOp operand) : base(operand) { }
        
        internal override EvalResult Eval() {
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                if (val.CompareTo(True) == 0)
                    return SingleTrue;
            }
            return SingleFalse;
        }
    }
    
    public class All : UnaryBoolOp
    {
        public override     string      ToString() => $"All({operand})";
        
        public All(BoolOp operand) : base(operand) { }
        
        internal override EvalResult Eval() {
            var eval = operand.Eval();
            foreach (var val in eval.values) {
                if (val.CompareTo(True) != 0)
                    return SingleFalse;
            }
            return SingleTrue;
        }
    }
    
    // ----------------------------------- (n-ary) logical group operators -----------------------------------
    public abstract class GroupBoolOp : BoolOp
    {
        protected           List<BoolOp>        operands;

        protected GroupBoolOp(List<BoolOp> operands) { this.operands = operands; }
        
        internal override void Init(OperatorContext cx) {
            cx.ValidateReuse(this); // results are reused
            foreach (var operand in operands) {
                operand.Init(cx);
            }
        }
    }
    
    public class And : GroupBoolOp
    {
        public override     string      ToString() => '(' + string.Join(" && ", operands) + ')';
        
        public And(List<BoolOp> operands) : base(operands) { }
        
        internal override EvalResult Eval() {
            var evalList = new List<EvalResult>(operands.Count);
            foreach (var operand in operands) {
                var eval = operand.Eval();
                evalList.Add(eval);
            }
            
            evalResult.Clear();
            var nAryResult = new N_aryResult(evalList);
            foreach (N_aryList result in nAryResult) {
                var itemResult = True;
                for (int n = 0; n < operands.Count; n++) {
                    if (result.evalResult.values[n].CompareTo(True) != 0) {
                        itemResult = False;
                        break;
                    }
                }
                evalResult.Add(itemResult);
            }
            return evalResult;
        }
    }
    
    public class Or : GroupBoolOp
    {
        public override     string      ToString() => '(' + string.Join(" || ", operands) + ')';
        
        public Or(List<BoolOp> operands) : base(operands) { }
        
        internal override EvalResult Eval() {
            var evalList = new List<EvalResult>(operands.Count);
            foreach (var operand in operands) {
                var eval = operand.Eval();
                evalList.Add(eval);
            }
            
            evalResult.Clear();
            var nAryResult = new N_aryResult(evalList);
            foreach (N_aryList result in nAryResult) {
                var itemResult = False;
                for (int n = 0; n < operands.Count; n++) {
                    if (result.evalResult.values[n].CompareTo(True) == 0) {
                        itemResult = True;
                        break;
                    }
                }
                evalResult.Add(itemResult);
            }
            return evalResult;
        }
    }
    
}