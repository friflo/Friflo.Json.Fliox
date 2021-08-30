// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // ----------------------------------- unary logical operations -----------------------------------
    public abstract class UnaryLogicalOp : FilterOperation
    {
        [Fri.Required]  public  FilterOperation     operand;     // e.g.   i => i.amount < 1

        protected UnaryLogicalOp() { }
        protected UnaryLogicalOp(FilterOperation operand) { this.operand = operand; }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx, 0);
        }
    }
    
    public class Not : UnaryLogicalOp
    {
        public override string      Linq => $"!({operand})";

        public Not() { }
        public Not(FilterOperation operand) : base(operand) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                evalResult.Add(val.CompareTo(True) == 0 ? False : True);
            }
            return evalResult;
        }
    }
    

    
    // ----------------------------------- (n-ary) logical group operations -----------------------------------
    public abstract class BinaryLogicalOp : FilterOperation
    {
        [Fri.Required]  public          List<FilterOperation>   operands;
        [Fri.Ignore] internal readonly  List<EvalResult>        evalList        = new List<EvalResult>();
        [Fri.Ignore] internal           N_aryResultEnumerator   resultIterator  = new N_aryResultEnumerator(true); // reused iterator

        protected BinaryLogicalOp() { }
        protected BinaryLogicalOp(List<FilterOperation> operands) { this.operands = operands; }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            foreach (var operand in operands) {
                operand.Init(cx, 0);
            }
        }
    }
    
    public class And : BinaryLogicalOp
    {
        public override string      Linq => string.Join(" && ", operands);

        public And() { }
        public And(List<FilterOperation> operands) : base(operands) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalList.Clear();
            foreach (var operand in operands) {
                var eval = operand.Eval(cx);
                evalList.Add(eval);
            }
            
            evalResult.Clear();
            var nAryResult = new N_aryResult(evalList);
            resultIterator.Init(nAryResult);
            while (resultIterator.MoveNext()) {
                var result = resultIterator.Current;
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
    
    public class Or : BinaryLogicalOp
    {
        public override string      Linq => string.Join(" || ", operands);
        
        public Or() { }
        public Or(List<FilterOperation> operands) : base(operands) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalList.Clear();
            foreach (var operand in operands) {
                var eval = operand.Eval(cx);
                evalList.Add(eval);
            }
            
            evalResult.Clear();
            var nAryResult = new N_aryResult(evalList);
            resultIterator.Init(nAryResult);
            while (resultIterator.MoveNext()) {
                var result = resultIterator.Current;
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