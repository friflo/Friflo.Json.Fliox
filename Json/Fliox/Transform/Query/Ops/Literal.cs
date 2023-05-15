// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
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
        public   override OpType    Type                    => OpType.STRING;
        internal override void      AppendLinq(AppendCx cx) { cx.Append("'"); AppendEscaped(cx.sb); cx.Append("'"); }

        public StringLiteral() { }
        public StringLiteral(string value) { this.value = value; }

        internal override Scalar Eval(EvalCx cx) {
            return new Scalar(value);
        }
        
        private void AppendEscaped(StringBuilder sb) {
            foreach (var c in value) {
                switch (c) {
                    case '\'':  sb.Append("\\'"); continue;  // single quote
                    case '\b':  sb.Append("\\b"); continue;  // backspace
                    case '\f':  sb.Append("\\f"); continue;  // form feed
                    case '\n':  sb.Append("\\n"); continue;  // new line
                    case '\r':  sb.Append("\\r"); continue;  // carriage return
                    case '\t':  sb.Append("\\t"); continue;  // horizontal tabulator
                    case '\v':  sb.Append("\\v"); continue;  // vertical tabulator
                }
                sb.Append(c);
            }
        }
    }
    
    public sealed class DoubleLiteral : Literal
    {
        public              double  value;
        internal override   bool    IsNumeric()             => true;
        public   override   string  OperationName           => value.ToString(CultureInfo.InvariantCulture);
        public   override OpType    Type                    => OpType.DOUBLE;
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
        public   override OpType    Type                    => OpType.INT64;
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
        public   override OpType    Type                    => OpType.TRUE;
        internal override void      AppendLinq(AppendCx cx) => cx.Append("true");

        internal override void Init(OperationContext cx) { }

        internal override Scalar Eval(EvalCx cx) {
            return True;
        }
    }
    
    public sealed class FalseLiteral : FilterOperation
    {
        public   override string    OperationName           => "false";
        public   override OpType    Type                    => OpType.FALSE;
        internal override void      AppendLinq(AppendCx cx) => cx.Append("false");

        internal override void Init(OperationContext cx) { }

        internal override Scalar Eval(EvalCx cx) {
            return False;
        }
    }

    public sealed class NullLiteral : Literal
    {
        public   override string    OperationName           => "null";
        public   override OpType    Type                    => OpType.NULL;
        internal override void      AppendLinq(AppendCx cx) => cx.Append("null");

        public NullLiteral() { }

        internal override Scalar Eval(EvalCx cx) {
            return Null;
        }
    }
}
