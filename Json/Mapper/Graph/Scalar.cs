// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public enum ScalarType {
        String,
        Number,
        Bool,
        Null,
        Array,
        Object
    }

    /// Note: Could be a readonly struct, but performance degrades and API gets unhandy if so.
    public class Scalar
    {
        internal    readonly    ScalarType      type;
        internal    readonly    string          stringValue; 
        internal    readonly    bool            isFloat;
        internal    readonly    double          doubleValue;
        internal    readonly    long            longValue;
        internal    readonly    bool            boolValue;
        

        public Scalar(ScalarType type, string value) {
            this.type   = type;
            stringValue = value;
        }
        
        public Scalar(string value) {
            type        = ScalarType.String;
            stringValue = value;
        }
        
        public Scalar(double value) {
            type        = ScalarType.Number;
            isFloat     = true;
            doubleValue = value;
        }
        
        public Scalar(long value) {
            type        = ScalarType.Number;
            isFloat     = false;
            longValue   = value;
        }

        public Scalar(bool value) {
            type        = ScalarType.Bool;
            boolValue   = value;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        public long CompareTo(Scalar other) {
            if (this == other)
                return 0;
            int typeDiff = type - other.type;
            if (typeDiff != 0)
                return typeDiff;
            switch (type) {
                case ScalarType.String:
                    return String.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                case ScalarType.Number:
                    if (isFloat) {
                        if (other.isFloat)
                            return (long) (doubleValue - other.doubleValue);
                        return (long) (doubleValue - other.longValue);
                    }
                    if (other.isFloat)
                        return (long) (longValue - other.doubleValue);
                    return longValue - other.longValue;
                case ScalarType.Bool:
                    long b1 = boolValue ? 1 : 0;
                    long b2 = other.boolValue ? 1 : 0;
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
                        return doubleValue;
                    return longValue;
                case ScalarType.String:
                    return stringValue;
                case ScalarType.Bool:
                    return boolValue;
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
                return new Scalar(Math.Abs(doubleValue));
            return     new Scalar(Math.Abs(longValue));
        }
        
        public Scalar Ceiling() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Ceiling(        doubleValue));
            return     new Scalar(Math.Ceiling((double)longValue));
        }
        
        public Scalar Floor() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Floor(        doubleValue));
            return     new Scalar(Math.Floor((double)longValue));
        }
        
        public Scalar Exp() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Exp(doubleValue));
            return     new Scalar(Math.Exp(longValue));
        }
        
        public Scalar Log() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Log(doubleValue));
            return     new Scalar(Math.Log(longValue));
        }
        
        public Scalar Sqrt() {
            AssertUnaryNumber();
            if (isFloat)
                return new Scalar(Math.Sqrt(doubleValue));
            return     new Scalar(Math.Sqrt(longValue));
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
                    return new Scalar(doubleValue + other.doubleValue);
                return     new Scalar(doubleValue + other.longValue);
            }
            if (other.isFloat)
                return     new Scalar(longValue   + other.doubleValue);
            return         new Scalar(longValue   + other.longValue);
        }
        
        public Scalar Subtract(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(doubleValue - other.doubleValue);
                return     new Scalar(doubleValue - other.longValue);
            }
            if (other.isFloat)
                return     new Scalar(longValue   - other.doubleValue);
            return         new Scalar(longValue   - other.longValue);
        }
        
        public Scalar Multiply(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(doubleValue * other.doubleValue);
                return     new Scalar(doubleValue * other.longValue);
            }
            if (other.isFloat)
                return     new Scalar(longValue   * other.doubleValue);
            return         new Scalar(longValue   * other.longValue);
        }
        
        public Scalar Divide(Scalar other) {
            AssertBinaryNumbers(other);
            if (isFloat) {
                if (other.isFloat)
                    return new Scalar(doubleValue / other.doubleValue);
                return     new Scalar(doubleValue / other.longValue);
            }
            if (other.isFloat)
                return     new Scalar(longValue   / other.doubleValue);
            return         new Scalar(longValue   / other.longValue);
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
                        sb.Append(doubleValue);
                    else
                        sb.Append(longValue);
                    break;
                case ScalarType.String:
                    sb.Append('\'');
                    sb.Append(stringValue);
                    sb.Append('\'');
                    break;
                case ScalarType.Bool:
                    sb.Append(boolValue ? "true": "false");
                    break;
                case ScalarType.Null:
                    sb.Append("null");
                    break;
            }
        }
    }

}