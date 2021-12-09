// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox.Transform.Query.Arity;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Contains : BinaryBoolOp
    {
        protected override void AppendLinq(StringBuilder sb) => sb.Append($"{left.Linq}.Contains({right.Linq})");

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
        protected override void AppendLinq(StringBuilder sb) => sb.Append($"{left.Linq}.StartsWith({right.Linq})");

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
        protected override void AppendLinq(StringBuilder sb) => sb.Append($"{left.Linq}.EndsWith({right.Linq})");

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