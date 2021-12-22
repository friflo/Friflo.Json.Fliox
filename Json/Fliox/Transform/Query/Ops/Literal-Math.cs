// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class PiLiteral : Literal
    {
        public   override string    OperationName => "PI";
        public   override void      AppendLinq(AppendCx cx) => cx.Append("PI");

        public PiLiteral() { }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Scalar.PI);
            return evalResult;
        }
    }
    
    public sealed class EulerLiteral : Literal
    {
        public   override string    OperationName => "E";
        public   override void      AppendLinq(AppendCx cx) => cx.Append("E");

        public EulerLiteral() { }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Scalar.E);
            return evalResult;
        }
    }
    
    public sealed class TauLiteral : Literal
    {
        public   override string    OperationName => "Tau";
        public   override void      AppendLinq(AppendCx cx) => cx.Append("Tau");

        public TauLiteral() { }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Scalar.Tau);
            return evalResult;
        }
    }
}