// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class PiLiteral : Literal
    {
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => "PI";
        internal override   void    AppendLinq(AppendCx cx) => cx.Append("PI");

        public PiLiteral() { }

        internal override Scalar Eval(EvalCx cx) {
            return Scalar.PI;
        }
    }
    
    public sealed class EulerLiteral : Literal
    {
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => "E";
        internal override   void    AppendLinq(AppendCx cx) => cx.Append("E");

        public EulerLiteral() { }

        internal override Scalar Eval(EvalCx cx) {
            return Scalar.E;
        }
    }
    
    public sealed class TauLiteral : Literal
    {
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => "Tau";
        internal override   void    AppendLinq(AppendCx cx) => cx.Append("Tau");

        public TauLiteral() { }

        internal override Scalar Eval(EvalCx cx) {
            return Scalar.Tau;
        }
    }
}