// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Flow.Graph
{
    public enum ScalarType : byte {
        Undefined,
        String,
        Double,
        Long,
        Bool,
        Null,
        Array,
        Object
    }

    public readonly struct Scalar
    {
        public      readonly    ScalarType      type;           // 1 byte - underlying type set to byte
        private     readonly    long            primitiveValue; // 8 bytes
        private     readonly    string          stringValue;    // 8 bytes

        private                 double          DoubleValue => BitConverter.Int64BitsToDouble(primitiveValue);
        private                 long            LongValue => primitiveValue;
        private                 bool            BoolValue => primitiveValue != 0;

        private                 bool            IsNumber => type == ScalarType.Double || type == ScalarType.Long;
        private                 bool            IsDouble => type == ScalarType.Double;
        private                 bool            IsLong   => type == ScalarType.Long;
        
        public static readonly Scalar           True  = new Scalar(true); 
        public static readonly Scalar           False = new Scalar(false);
        
        public static readonly Scalar           Null  = new Scalar(ScalarType.Null, null);


        internal Scalar(ScalarType type, string value) {
            this.type       = type;
            stringValue     = value;
            //
            primitiveValue  = 0;
        }
        
        public Scalar(string value) {
            type            = ScalarType.String;
            stringValue     = value;
            //
            primitiveValue  = 0;
        }
        
        public Scalar(double value) {
            type            = ScalarType.Double;
            primitiveValue  = BitConverter.DoubleToInt64Bits(value);
            //
            stringValue     = null;
        }
        
        public Scalar(long value) {
            type            = ScalarType.Long;
            primitiveValue  = value;
            //
            stringValue     = null;
        }

        public Scalar(bool value) {
            type            = ScalarType.Bool;
            primitiveValue  = value ? 1 : 0;
            //
            stringValue     = null;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }
        
        // --- value access methods ---
        public string AsString() {
            if (type == ScalarType.String)
                return stringValue;
            throw new InvalidOperationException($"Scalar cannot be returned as string. type: {type}, value: {this}");
        }
        
        public double AsDouble() {
            if (type == ScalarType.Double)
                return DoubleValue;
            throw new InvalidOperationException($"Scalar cannot be returned as double. type: {type}, value: {this}");
        }
        
        public long AsLong() {
            if (type == ScalarType.Long)
                return LongValue;
            throw new InvalidOperationException($"Scalar cannot be returned as long. type: {type}, value: {this}");
        }
        
        public bool AsBool() {
            if (type == ScalarType.Bool)
                return BoolValue;
            throw new InvalidOperationException($"Scalar cannot be returned as bool. type: {type}, value: {this}");
        }

        public object AsObject() {
            switch (type) {
                case ScalarType.Double:
                    return DoubleValue;
                case ScalarType.Long:
                    return LongValue;
                case ScalarType.String:
                    return stringValue;
                case ScalarType.Bool:
                    return BoolValue;
                case ScalarType.Null:
                    return null;
                case ScalarType.Object:
                case ScalarType.Array:
                    return stringValue;
                default:
                    throw new NotImplementedException($"value type supported. type: {type}");
            }
        }

        // --- compare two scalars ---
        public long CompareTo(Scalar other) {
            int typeDiff;
            switch (type) {
                case ScalarType.String:
                    typeDiff = ScalarType.String - other.type;
                    if (typeDiff != 0)
                        return typeDiff;
                    return String.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                case ScalarType.Double:
                    if (other.IsDouble)
                        return (long) (DoubleValue - other.DoubleValue);
                    if (other.IsLong)
                        return (long) (DoubleValue - other.LongValue);
                    return ScalarType.Double - other.type;
                case ScalarType.Long:
                    if (other.IsDouble)
                        return (long) (LongValue - other.DoubleValue);
                    if (other.IsLong)
                        return LongValue - other.LongValue;
                    return ScalarType.Long - other.type;
                case ScalarType.Bool:
                    typeDiff = ScalarType.Bool - other.type;
                    // possible primitive values: 0 or 1
                    return typeDiff != 0 ? typeDiff : primitiveValue - other.primitiveValue;
                case ScalarType.Null:
                    typeDiff = ScalarType.Null - other.type;
                    return typeDiff != 0 ? typeDiff : 0;
                default:
                    throw new NotSupportedException($"Scalar does not support CompareTo() for type: {type}");                
            }
        }

        // --- unary arithmetic operators ---
        public Scalar Abs() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Abs(DoubleValue));
            return     new Scalar(Math.Abs(LongValue));
        }
        
        public Scalar Ceiling() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Ceiling(        DoubleValue));
            return     new Scalar(Math.Ceiling((double)LongValue));
        }
        
        public Scalar Floor() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Floor(        DoubleValue));
            return     new Scalar(Math.Floor((double)LongValue));
        }
        
        public Scalar Exp() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Exp(DoubleValue));
            return     new Scalar(Math.Exp(LongValue));
        }
        
        public Scalar Log() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Log(DoubleValue));
            return     new Scalar(Math.Log(LongValue));
        }
        
        public Scalar Sqrt() {
            AssertUnaryNumber();
            if (IsDouble)
                return new Scalar(Math.Sqrt(DoubleValue));
            return     new Scalar(Math.Sqrt(LongValue));
        }

        private void AssertUnaryNumber() {
            if (!IsNumber)
                throw new InvalidOperationException($"Expect operand being numeric. operand: {this}");
        }
        
        // --- binary arithmetic operators ---
        public Scalar Add(Scalar other) {
            AssertBinaryNumbers(other);
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue + other.DoubleValue);
                return     new Scalar(DoubleValue + other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   + other.DoubleValue);
            return         new Scalar(LongValue   + other.LongValue);
        }
        
        public Scalar Subtract(Scalar other) {
            AssertBinaryNumbers(other);
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue - other.DoubleValue);
                return     new Scalar(DoubleValue - other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   - other.DoubleValue);
            return         new Scalar(LongValue   - other.LongValue);
        }
        
        public Scalar Multiply(Scalar other) {
            AssertBinaryNumbers(other);
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue * other.DoubleValue);
                return     new Scalar(DoubleValue * other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   * other.DoubleValue);
            return         new Scalar(LongValue   * other.LongValue);
        }
        
        public Scalar Divide(Scalar other) {
            AssertBinaryNumbers(other);
            if (IsDouble) {
                if (other.IsDouble)
                    return new Scalar(DoubleValue / other.DoubleValue);
                return     new Scalar(DoubleValue / other.LongValue);
            }
            if (other.IsDouble)
                return     new Scalar(LongValue   / other.DoubleValue);
            return         new Scalar(LongValue   / other.LongValue);
        }
        
        private void AssertBinaryNumbers(Scalar other) {
            if (!IsNumber || !other.IsNumber)
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
                case ScalarType.Double:
                    sb.Append(DoubleValue);
                    break;
                case ScalarType.Long:
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
                case ScalarType.Undefined:
                    sb.Append("(Undefined)");
                    break;
            }
        }
    }

}