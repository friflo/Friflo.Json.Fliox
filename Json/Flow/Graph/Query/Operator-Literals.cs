// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;

namespace Friflo.Json.Flow.Graph.Query
{
    // --- literals
    public abstract class Literal : Operator {
        // is set always to the same value in Eval() so it can be reused
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        internal override void Init(OperatorContext cx) { }
    }
        
    public class StringLiteral : Literal
    {
        public              string      value;
        
        public override     string      ToString() => $"'{value}'";

        public StringLiteral(string value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class DoubleLiteral : Literal
    {
        private             double      value;

        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);

        public DoubleLiteral(double value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class LongLiteral : Literal
    {
        private             long      value;

        public override     string      ToString() => value.ToString();

        public LongLiteral(long value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public class BoolLiteral : Literal
    {
        public bool         value;
        
        public override     string      ToString() => value ? "true" : "false";

        public BoolLiteral(bool value) {
            this.value = value;
        }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(value ? True : False);
            return evalResult;
        }
    }

    public class NullLiteral : Literal
    {
        public override     string      ToString() => "null";


        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Null);
            return evalResult;
        }
    }
}
