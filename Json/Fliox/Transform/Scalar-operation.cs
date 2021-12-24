// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Friflo.Json.Fliox.Transform
{
    public readonly partial struct Scalar
    {
        // ------------------------------- unary arithmetic operations -------------------------------
        public Scalar Abs(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Abs(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Abs(LongValue));
            } 
            return ExpectNumber(operation);
        }
        
        public Scalar Ceiling(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Ceiling(DoubleValue));
                case ScalarType.Long:   return new Scalar(             LongValue);
            } 
            return ExpectNumber(operation);
        }
        
        public Scalar Floor(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Floor(DoubleValue));
                case ScalarType.Long:   return new Scalar(           LongValue);
            } 
            return ExpectNumber(operation);
        }
        
        public Scalar Exp(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Exp(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Exp(LongValue));
            } 
            return ExpectNumber(operation);
        }
        
        public Scalar Log(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Log(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Log(LongValue));
            } 
            return ExpectNumber(operation);
        }
        
        public Scalar Sqrt(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Sqrt(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Sqrt(LongValue));
            }
            return ExpectNumber(operation);
        }

        private Scalar ExpectNumber(Operation operation) {
            var sb = new StringBuilder();
            sb.Append("expect numeric operand. was: ");
            AppendTo(sb);
            if (operation != null) {
                var appendCx = new AppendCx(sb);
                sb.Append(" in ");
                operation.AppendLinq(appendCx);
            }
            return Error(sb.ToString());
        }
        
        // ------------------------------- binary arithmetic operations -------------------------------
        public Scalar Add(in Scalar other, Operation operation) {
            if (!AssertBinaryNumbers(other, operation, out Scalar error))
                return error;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue + other.DoubleValue);
                return     new Scalar(DoubleValue + other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   + other.DoubleValue);
            return         new Scalar(LongValue   + other.LongValue);
        }
        
        public Scalar Subtract(in Scalar other, Operation operation) {
            if (!AssertBinaryNumbers(other, operation, out Scalar error))
                return error;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue - other.DoubleValue);
                return     new Scalar(DoubleValue - other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   - other.DoubleValue);
            return         new Scalar(LongValue   - other.LongValue);
        }
        
        public Scalar Multiply(in Scalar other, Operation operation) {
            if (!AssertBinaryNumbers(other, operation, out Scalar error))
                return error;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue * other.DoubleValue);
                return     new Scalar(DoubleValue * other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   * other.DoubleValue);
            return         new Scalar(LongValue   * other.LongValue);
        }
        
        public Scalar Divide(in Scalar other, Operation operation) {
            if (!AssertBinaryNumbers(other, operation, out Scalar error))
                return error;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue / other.DoubleValue);
                return     new Scalar(DoubleValue / other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   / other.DoubleValue);
            return         new Scalar(LongValue   / other.LongValue);
        }
        
        private bool AssertBinaryNumbers(in Scalar other, Operation operation, out Scalar error) {
            if (IsNumber && other.IsNumber) {
                error = default;
                return true;
            }
            var sb = new StringBuilder();
            sb.Append("expect numeric operands. left: ");
            AppendTo(sb);
            sb.Append(", right: ");
            other.AppendTo(sb);
            if (operation != null) {
                sb.Append(" in ");
                var cx = new AppendCx(sb);
                operation.AppendLinq(cx);
            }
            error = Error(sb.ToString());
            return false;
        }
        
        // ------------------------------- binary string operations -------------------------------
        public Scalar Contains(in Scalar other, Operation operation) {
            if (!AssertBinaryString(other, operation, out Scalar error))
                return error;
            return stringValue.Contains(other.stringValue) ? True : False;
        }
        
        public Scalar StartsWith(in Scalar other, Operation operation) {
            if (!AssertBinaryString(other, operation, out Scalar error))
                return error;
            return stringValue.StartsWith(other.stringValue) ? True : False;
        }
        
        public Scalar EndsWith(in Scalar other, Operation operation) {
            if (!AssertBinaryString(other, operation, out Scalar error))
                return error;
            return stringValue.EndsWith(other.stringValue) ? True : False;
        }
        
        private bool AssertBinaryString(in Scalar other, Operation operation, out Scalar error) {
            if (IsString && other.IsString) {
                error = default;
                return true;
            }
            var sb = new StringBuilder();
            sb.Append("expect string operands. left: ");
            AppendTo(sb);
            sb.Append(", right: ");
            other.AppendTo(sb);
            if (operation != null) {
                sb.Append(" in ");
                var cx = new AppendCx(sb);
                operation.AppendLinq(cx);
            }
            error = Error(sb.ToString());
            return false;
        }
    }
}