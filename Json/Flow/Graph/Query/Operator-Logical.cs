// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Query.Arity;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Graph.Query
{
    [Fri.Discriminator("op")]
    // --- BoolOp  
    [Fri.Polymorph(typeof(Equal),               Discriminant = "equal")]
    [Fri.Polymorph(typeof(NotEqual),            Discriminant = "notEqual")]
    [Fri.Polymorph(typeof(LessThan),            Discriminant = "lessThan")]
    [Fri.Polymorph(typeof(LessThanOrEqual),     Discriminant = "lessThanOrEqual")]
    [Fri.Polymorph(typeof(GreaterThan),         Discriminant = "greaterThan")]
    [Fri.Polymorph(typeof(GreaterThanOrEqual),  Discriminant = "greaterThanOrEqual")]
    //
    [Fri.Polymorph(typeof(And),                 Discriminant = "and")]
    [Fri.Polymorph(typeof(Or),                  Discriminant = "or")]
    //
    [Fri.Polymorph(typeof(Not),                 Discriminant = "not")]
    [Fri.Polymorph(typeof(Any),                 Discriminant = "any")]
    [Fri.Polymorph(typeof(All),                 Discriminant = "all")]
    
    // ----------------------------- BoolOp --------------------------
    public abstract class BoolOp : Operator
    {
        [Fri.Ignore]
        internal readonly  EvalResult   evalResult = new EvalResult(new List<Scalar>());

        public JsonFilter Filter() {
            return new JsonFilter(this);
        }
    }
    
    // ----------------------------------- unary logical operators -----------------------------------
    public abstract class UnaryLogicalOp : BoolOp
    {
        public           BoolOp              operand;     // e.g.   i => i.amount < 1

        protected UnaryLogicalOp() { }
        protected UnaryLogicalOp(BoolOp operand) { this.operand = operand; }
        
        internal override void Init(OperatorContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            operand.Init(cx, 0);
        }
    }
    
    public class Not : UnaryLogicalOp
    {
        public override     string      ToString() => $"!({operand})";

        public Not() { }
        public Not(BoolOp operand) : base(operand) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = operand.Eval(cx);
            foreach (var val in eval.values) {
                evalResult.Add(val.CompareTo(True) == 0 ? False : True);
            }
            return evalResult;
        }
    }
    

    
    // ----------------------------------- (n-ary) logical group operators -----------------------------------
    public abstract class BinaryLogicalOp : BoolOp
    {
        public              List<BoolOp>            operands;
        [Fri.Ignore]
        internal readonly   List<EvalResult>        evalList        = new List<EvalResult>();
        [Fri.Ignore]
        internal            N_aryResultEnumerator   resultIterator  = new N_aryResultEnumerator(true); // reused iterator

        protected BinaryLogicalOp() { }
        protected BinaryLogicalOp(List<BoolOp> operands) { this.operands = operands; }
        
        internal override void Init(OperatorContext cx, InitFlags flags) {
            cx.ValidateReuse(this); // results are reused
            foreach (var operand in operands) {
                operand.Init(cx, 0);
            }
        }
    }
    
    public class And : BinaryLogicalOp
    {
        public override     string      ToString() => string.Join(" && ", operands);

        public And() { }
        public And(List<BoolOp> operands) : base(operands) { }
        
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
        public override     string      ToString() => string.Join(" || ", operands);
        
        public Or() { }
        public Or(List<BoolOp> operands) : base(operands) { }
        
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