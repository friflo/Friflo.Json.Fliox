// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Transform.Query.Ops
{
    // --- literals
    public abstract class Literal : Operation {
        // is set always to the same value in Eval() so it can be reused
        [Fri.Ignore]
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        internal override void Init(OperationContext cx, InitFlags flags) { }
    }
        
    public class StringLiteral : Literal
    {
        public              string      value;
        
        public override     string      Linq => $"'{value}'";

        public StringLiteral() { }
        public StringLiteral(string value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class DoubleLiteral : Literal
    {
        public          double      value;

        public override string      Linq => value.ToString(CultureInfo.InvariantCulture);

        public DoubleLiteral() { }
        public DoubleLiteral(double value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class LongLiteral : Literal
    {
        public          long        value;

        public override string      Linq => value.ToString();

        public LongLiteral() { }
        public LongLiteral(long value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class TrueLiteral : FilterOperation
    {
        public override string      Linq => "true";

        internal override void Init(OperationContext cx, InitFlags flags) { }

        internal override EvalResult Eval(EvalCx cx) {
            return SingleTrue;
        }
    }
    
    public class FalseLiteral : FilterOperation
    {
        public override string      Linq => "false";

        internal override void Init(OperationContext cx, InitFlags flags) { }

        internal override EvalResult Eval(EvalCx cx) {
            return SingleFalse;
        }
    }

    public class NullLiteral : Literal
    {
        public override string      Linq => "null";

        public NullLiteral() { }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Null);
            return evalResult;
        }
    }
}
