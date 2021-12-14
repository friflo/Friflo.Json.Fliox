// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Contains : BinaryBoolOp
    {
        public   override void AppendLinq(AppendCx cx) => AppendLinqMethod("Contains", left, right, cx);

        public Contains() { }
        public Contains(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var contains = pair.left.Contains(pair.right);
                evalResult.Add(contains);
            }
            return evalResult;
        }
    }
    
    public sealed class StartsWith : BinaryBoolOp
    {
        public   override void AppendLinq(AppendCx cx) => AppendLinqMethod("StartsWith", left, right, cx);

        public StartsWith() { }
        public StartsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var startsWith = pair.left.StartsWith(pair.right);
                evalResult.Add(startsWith);
            }
            return evalResult;
        }
    }
    
    public sealed class EndsWith : BinaryBoolOp
    {
        public   override void AppendLinq(AppendCx cx) => AppendLinqMethod("EndsWith", left, right, cx);

        public EndsWith() { }
        public EndsWith(Operation left, Operation right) : base(left, right) { }
        
        internal override EvalResult Eval(EvalCx cx) {
            evalResult.Clear();
            var eval = new BinaryResult(left.Eval(cx), right.Eval(cx));
            foreach (var pair in eval) {
                var endsWith = pair.left.EndsWith(pair.right);
                evalResult.Add(endsWith);
            }
            return evalResult;
        }
    }
}