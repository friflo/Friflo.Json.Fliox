// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public enum ScalarType : byte {
        String,
        Number,
        Bool,
        Null,
        Array,
        Object
    }

    public readonly struct Scalar
    {
        private     readonly    ScalarType      type;           // 1 byte - underlying type set to byte
        private     readonly    bool            isFloat;        // 1 byte - usually)
        private     readonly    long            primitiveValue; // 8 bytes
        internal    readonly    string          stringValue;    // 8 bytes

        private                 double          DoubleValue => BitConverter.Int64BitsToDouble(primitiveValue);
        private                 long            LongValue => primitiveValue;
        private                 bool            BoolValue => primitiveValue != 0;
        

        public Scalar(ScalarType type, string value) {
            this.type       = type;
            stringValue     = value;
            //
            isFloat = false;
            primitiveValue  = 0;
        }
        
        public Scalar(string value) {
            type            = ScalarType.String;
            stringValue     = value;
            //
            isFloat = false;
            primitiveValue  = 0;
        }
        
        public Scalar(double value) {
            type            = ScalarType.Number;
            isFloat         = true;
            primitiveValue  = BitConverter.DoubleToInt64Bits(value);
            //
            stringValue     = null;
        }
        
        public Scalar(long value) {
            type            = ScalarType.Number;
            isFloat         = false;
            primitiveValue  = value;
            //
            stringValue     = null;
        }

        public Scalar(bool value) {
            type            = ScalarType.Bool;
            primitiveValue  = value ? 1 : 0;
            //
            isFloat         = false;
            stringValue     = null;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        public long CompareTo(Scalar other) {
            int typeDiff = type - other.type;
            if (typeDiff != 0)
                return typeDiff;
            switch (type) {
                case ScalarType.String:
                    return String.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                case ScalarType.Number:
                    if (isFloat) {
                        if (other.isFloat)
                            return (long) (DoubleValue - other.DoubleValue);
                        return (long) (DoubleValue - other.LongValue);
                    }
                    if (other.isFloat)
                        return (long) (LongValue - other.DoubleValue);
                    return LongValue - other.LongValue;
                case ScalarType.Bool:
                    long b1 = BoolValue ? 1 : 0;
                    long b2 = other.BoolValue ? 1 : 0;
                    return b1 - b2;
                case ScalarType.Null:
                    return 0;
                default:
                    throw new NotSupportedException($"SelectorValue does not support Compare for: {type}");                
            }
        }

        public object AsObject() {
            switch (type) {
                case ScalarType.Number:
                    if (isFloat)
                        return DoubleValue;
                    return LongValue;
                case ScalarType.String:
                    return stringValue;
                case ScalarType.Bool:
                    return BoolValue;
                case ScalarType.Null:
                    return null;
                default:
                    throw new NotImplementedException($"value type supported. type: {type}");
            }
        }


        
        // --- unary arithmetic operators ---
        public Scalar Abs() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Abs(DoubleValue));
            return     new Scalar(Math.Abs(LongValue));
        }
        
        public Scalar Ceiling() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Ceiling(        DoubleValue));
            return     new Scalar(Math.Ceiling((double)LongValue));
        }
        
        public Scalar Floor() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Floor(        DoubleValue));
            return     new Scalar(Math.Floor((double)LongValue));
        }
        
        public Scalar Exp() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Exp(DoubleValue));
            return     new Scalar(Math.Exp(LongValue));
        }
        
        public Scalar Log() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Log(DoubleValue));
            return     new Scalar(Math.Log(LongValue));
        }
        
        public Scalar Sqrt() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Sqrt(DoubleValue));
            return     new Scalar(Math.Sqrt(LongValue));
        }

        private void AssertUnaryNumber() {
            if (type != ScalarType.Number)
                throw new InvalidOperationException($"Expect operand being numeric. operand: {this}");
        }
        
        // --- binary arithmetic operators ---
        public Scalar Add(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(DoubleValue + other.DoubleValue);
                return     new Scalar(DoubleValue + other.LongValue);
            }
            if (other.isFloat)
                return     new Scalar(LongValue   + other.DoubleValue);
            return         new Scalar(LongValue   + other.LongValue);
        }
        
        public Scalar Subtract(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(DoubleValue - other.DoubleValue);
                return     new Scalar(DoubleValue - other.LongValue);
            }
            if (other.isFloat)
                return     new Scalar(LongValue   - other.DoubleValue);
            return         new Scalar(LongValue   - other.LongValue);
        }
        
        public Scalar Multiply(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(DoubleValue * other.DoubleValue);
                return     new Scalar(DoubleValue * other.LongValue);
            }
            if (other.isFloat)
                return     new Scalar(LongValue   * other.DoubleValue);
            return         new Scalar(LongValue   * other.LongValue);
        }
        
        public Scalar Divide(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(DoubleValue / other.DoubleValue);
                return     new Scalar(DoubleValue / other.LongValue);
            }
            if (other.isFloat)
                return     new Scalar(LongValue   / other.DoubleValue);
            return         new Scalar(LongValue   / other.LongValue);
        }
        
        private void AssertBinaryNumbers(Scalar other) {
            if (type != ScalarType.Number || other.type != ScalarType.Number)
                throw new InvalidOperationException($"Expect both operands being numeric. left: {this}, right: {other}");
        }
        
        // --------

        /// Format as debug string - not as JSON
        internal void AppendTo(StringBuilder sb) {
            switch (type) {
                case ScalarType.Array:
                case ScalarType.Object:
                    sb.Append(stringValue);
                    break;
                case ScalarType.Number:
                    if (isFloat)
                        sb.Append(DoubleValue);
                    else
                        sb.Append(LongValue);
                    break;
                case ScalarType.String:
                    sb.Append('\'');
                    sb.Append(stringValue);
                    sb.Append('\'');
                    break;
                case ScalarType.Bool:
                    sb.Append(BoolValue ? "true": "false");
                    break;
                case ScalarType.Null:
                    sb.Append("null");
                    break;
            }
        }
    }

}