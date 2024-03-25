// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Transform.Query;

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
        
        public Scalar Length(Operation operation) {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.String: return new Scalar(stringValue.Length);
            }
            return ExpectString(operation);
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
        
        private Scalar ExpectString(Operation operation) {
            var sb = new StringBuilder();
            sb.Append("expect string operand. was: ");
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
            if (!AreNumbers(other, operation, out Scalar result))
                return result;
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
            if (!AreNumbers(other, operation, out Scalar result))
                return result;
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
            if (!AreNumbers(other, operation, out Scalar result))
                return result;
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
            if (!AreNumbers(other, operation, out Scalar result))
                return result;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue / other.DoubleValue);
                return     new Scalar(DoubleValue / other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   / other.DoubleValue);
            return         new Scalar(LongValue   / other.LongValue);
        }
        
        public Scalar Modulo(in Scalar other, Operation operation) {
            if (!AreNumbers(other, operation, out Scalar result))
                return result;
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue % other.DoubleValue);
                return     new Scalar(DoubleValue % other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   % other.DoubleValue);
            return         new Scalar(LongValue   % other.LongValue);
        }
        
        private bool AreNumbers(in Scalar other, Operation operation, out Scalar result) {
            if (IsNumber && other.IsNumber) {
                result = default;
                return true;
            }
            if (IsNull || other.IsNull) {
                result = Null;
                return false;
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
            result = Error(sb.ToString());
            return false;
        }
        
        // ------------------------------- binary string operations -------------------------------
        public Scalar Contains(in Scalar other, Operation operation) {
            if (!AreStrings(other, operation, out Scalar result))
                return result;
            return stringValue.Contains(other.stringValue) ? True : False;
        }
        
        public Scalar StartsWith(in Scalar other, Operation operation) {
            if (!AreStrings(other, operation, out Scalar result))
                return result;
            return stringValue.StartsWith(other.stringValue) ? True : False;
        }
        
        public Scalar EndsWith(in Scalar other, Operation operation) {
            if (!AreStrings(other, operation, out Scalar result))
                return result;
            return stringValue.EndsWith(other.stringValue) ? True : False;
        }
        
        private bool AreStrings(in Scalar other, Operation operation, out Scalar result) {
            if (IsString && other.IsString) {
                result = default;
                return true;
            }
            if (IsNull || other.IsNull) {
                result = Null;
                return false;
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
            result = Error(sb.ToString());
            return false;
        }
    }
}