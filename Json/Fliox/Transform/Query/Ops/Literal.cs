// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable EmptyConstructor
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // --- literals
    public abstract class Literal : Operation {
        // is set always to the same value in Eval() so it can be reused
        [Fri.Ignore]
        internal  readonly  EvalResult          evalResult = new EvalResult(new List<Scalar> {new Scalar()});
        
        internal override void Init(OperationContext cx, InitFlags flags) { }
    }
        
    public sealed class StringLiteral : Literal
    {
        [Fri.Required]  public  string      value;
        
        public   override void AppendLinq(AppendCx cx) { cx.Append("'"); cx.Append(value); cx.Append("'"); }

        public StringLiteral() { }
        public StringLiteral(string value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public sealed class DoubleLiteral : Literal
    {
        public              double      value;
        internal override   bool        IsNumeric => true;

        public   override void AppendLinq(AppendCx cx) => cx.Append(value.ToString(CultureInfo.InvariantCulture));

        public DoubleLiteral() { }
        public DoubleLiteral(double value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public sealed class LongLiteral : Literal
    {
        public              long        value;
        internal override   bool        IsNumeric => true;

        public   override void AppendLinq(AppendCx cx) => cx.sb.Append(value);

        public LongLiteral() { }
        public LongLiteral(long value) { this.value = value; }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(new Scalar(value));
            return evalResult;
        }
    }
    
    public sealed class TrueLiteral : FilterOperation
    {
        public   override void AppendLinq(AppendCx cx) => cx.Append("true");

        internal override void Init(OperationContext cx, InitFlags flags) { }

        internal override EvalResult Eval(EvalCx cx) {
            return SingleTrue;
        }
    }
    
    public sealed class FalseLiteral : FilterOperation
    {
        public   override void AppendLinq(AppendCx cx) => cx.Append("false");

        internal override void Init(OperationContext cx, InitFlags flags) { }

        internal override EvalResult Eval(EvalCx cx) {
            return SingleFalse;
        }
    }

    public sealed class NullLiteral : Literal
    {
        public   override void AppendLinq(AppendCx cx) => cx.Append("null");

        public NullLiteral() { }

        internal override EvalResult Eval(EvalCx cx) {
            evalResult.SetSingle(Null);
            return evalResult;
        }
    }
}
