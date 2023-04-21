// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Globalization;

// ReSharper disable EmptyConstructor
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    // --- literals
    public abstract class Literal : Operation {
        internal override void Init(OperationContext cx) { }
    }
        
    public sealed class StringLiteral : Literal
    {
        [Required]  public  string      value;
        
        public   override string    OperationName           => value;
        internal override void      AppendLinq(AppendCx cx) { cx.Append("'"); cx.Append(value); cx.Append("'"); }

        public StringLiteral() { }
        public StringLiteral(string value) { this.value = value; }

        internal override Scalar Eval(EvalCx cx) {
            return new Scalar(value);
        }
    }
    
    public sealed class DoubleLiteral : Literal
    {
        public              double  value;
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => value.ToString(CultureInfo.InvariantCulture);
        internal override   void    AppendLinq(AppendCx cx) => cx.Append(value.ToString(CultureInfo.InvariantCulture));

        public DoubleLiteral() { }
        public DoubleLiteral(double value) { this.value = value; }

        internal override Scalar Eval(EvalCx cx) {
            return new Scalar(value);
        }
    }
    
    public sealed class LongLiteral : Literal
    {
        public              long    value;
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => value.ToString();
        internal override   void    AppendLinq(AppendCx cx) => cx.sb.Append(value);

        public LongLiteral() { }
        public LongLiteral(long value) { this.value = value; }

        internal override Scalar Eval(EvalCx cx) {
            return new Scalar(value);
        }
    }
    
    public sealed class TrueLiteral : FilterOperation
    {
        public   override string    OperationName           => "true";
        internal override void      AppendLinq(AppendCx cx) => cx.Append("true");

        internal override void Init(OperationContext cx) { }

        internal override Scalar Eval(EvalCx cx) {
            return True;
        }
    }
    
    public sealed class FalseLiteral : FilterOperation
    {
        public   override string    OperationName           => "false";
        internal override void      AppendLinq(AppendCx cx) => cx.Append("false");

        internal override void Init(OperationContext cx) { }

        internal override Scalar Eval(EvalCx cx) {
            return False;
        }
    }

    public sealed class NullLiteral : Literal
    {
        public   override string    OperationName           => "null";
        internal override void      AppendLinq(AppendCx cx) => cx.Append("null");

        public NullLiteral() { }

        internal override Scalar Eval(EvalCx cx) {
            return Null;
        }
    }
}
