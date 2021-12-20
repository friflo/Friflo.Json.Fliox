// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
namespace Friflo.Json.Fliox.Transform
{
    public enum ScalarType : byte {
        Undefined,
        Error,
        //
        String,
        Double,
        Long,
        Null, // enhance case performance by putting it directly to Double & Long
        Bool,
        Array,
        Object
    }

#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public readonly struct Scalar
    {
        public      readonly    ScalarType      type;           // 1 byte - underlying type set to byte
        private     readonly    long            primitiveValue; // 8 bytes
        private     readonly    string          stringValue;    // 8 bytes

        private                 double          DoubleValue => BitConverter.Int64BitsToDouble(primitiveValue);
        private                 long            LongValue   => primitiveValue;
        private                 bool            BoolValue   => primitiveValue != 0;
        internal                string          ErrorMessage=> stringValue;

        private                 bool            IsString    => type == ScalarType.String;
        private                 bool            IsNumber    => type == ScalarType.Double || type == ScalarType.Long;
        private                 bool            IsDouble    => type == ScalarType.Double;
        private                 bool            IsLong      => type == ScalarType.Long;
        internal                bool            IsError     => type == ScalarType.Error;
        internal                bool            IsDefined   => type >  ScalarType.Error;
        
        public static readonly  Scalar          True    = new Scalar(true); 
        public static readonly  Scalar          False   = new Scalar(false);

        // ReSharper disable once InconsistentNaming
        public static readonly  Scalar          PI      = new Scalar(Math.PI);
        public static readonly  Scalar          E       = new Scalar(Math.E);
        public static readonly  Scalar          Tau     = new Scalar(6.2831853071795862); // Math.Tau
        
        public static readonly  Scalar          Null  = new Scalar(ScalarType.Null, null);


        internal Scalar(ScalarType type, string value) {
            this.type       = type;
            stringValue     = value;
            //
            primitiveValue  = 0;
        }
        
        internal static Scalar Error(string message) {
            return new Scalar(ScalarType.Error, message);
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
            if (type == ScalarType.Long)
                return LongValue.ToString();
            if (type == ScalarType.Null)
                return null;
            throw new InvalidOperationException($"Scalar cannot be returned as string. type: {type}, value: {this}");
        }
        
        public JsonKey AsJsonKey() {
            if (type == ScalarType.String)
                return new JsonKey(stringValue);
            if (type == ScalarType.Long)
                return new JsonKey(LongValue);
            if (type == ScalarType.Null)
                return new JsonKey();
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
        public long CompareTo(in Scalar other) {
            int typeDiff;
            switch (type) {
                case ScalarType.String:
                    typeDiff = ScalarType.String - other.type;
                    if (typeDiff != 0)
                        return typeDiff;
                    return string.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                case ScalarType.Double:
                    if (other.IsDouble)
                        return CompareDouble(DoubleValue, other.DoubleValue);
                    if (other.IsLong) {
                        return CompareDouble(DoubleValue, other.LongValue);
                    }
                    return ScalarType.Double - other.type;
                case ScalarType.Long:
                    if (other.IsDouble)
                        return CompareDouble(LongValue, other.DoubleValue);
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
        
        private static int CompareDouble(double left, double right) {
            double dif = left - right;
            return dif < 0 ? -1 : (dif > 0 ? +1 : 0);
        }

        // --- unary arithmetic operations ---
        public Scalar Abs() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Abs(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Abs(LongValue));
            } 
            return ExpectNumber();
        }
        
        public Scalar Ceiling() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Ceiling(DoubleValue));
                case ScalarType.Long:   return new Scalar(             LongValue);
            } 
            return ExpectNumber();
        }
        
        public Scalar Floor() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Floor(DoubleValue));
                case ScalarType.Long:   return new Scalar(           LongValue);
            } 
            return ExpectNumber();
        }
        
        public Scalar Exp() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Exp(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Exp(LongValue));
            } 
            return ExpectNumber();
        }
        
        public Scalar Log() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Log(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Log(LongValue));
            } 
            return ExpectNumber();
        }
        
        public Scalar Sqrt() {
            switch (type) {
                case ScalarType.Null:   return Null;
                case ScalarType.Double: return new Scalar(Math.Sqrt(DoubleValue));
                case ScalarType.Long:   return new Scalar(Math.Sqrt(LongValue));
            }
            return ExpectNumber();
        }

        private Scalar ExpectNumber() {
            return Error($"expect numeric operand. was: {this}");
        }
        
        // --- binary arithmetic operations ---
        public Scalar Add(in Scalar other) {
            if (!AssertBinaryNumbers(other, out Scalar error))
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
        
        public Scalar Subtract(in Scalar other) {
            if (!AssertBinaryNumbers(other, out Scalar error))
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
        
        public Scalar Multiply(in Scalar other) {
            if (!AssertBinaryNumbers(other, out Scalar error))
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
        
        public Scalar Divide(in Scalar other) {
            if (!AssertBinaryNumbers(other, out Scalar error))
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
        
        private bool AssertBinaryNumbers(in Scalar other, out Scalar error) {
            if (IsNumber && other.IsNumber) {
                error = default;
                return true;
            }
            error = Error($"expect two numeric operands. left: {this}, right: {other}");
            return false;
        }
        
        // --- binary string expressions
        public Scalar Contains(in Scalar other) {
            if (!AssertBinaryString(other, out Scalar error))
                return error;
            return stringValue.Contains(other.stringValue) ? True : False;
        }
        
        public Scalar StartsWith(in Scalar other) {
            if (!AssertBinaryString(other, out Scalar error))
                return error;
            return stringValue.StartsWith(other.stringValue) ? True : False;
        }
        
        public Scalar EndsWith(in Scalar other) {
            if (!AssertBinaryString(other, out Scalar error))
                return error;
            return stringValue.EndsWith(other.stringValue) ? True : False;
        }
        
        private bool AssertBinaryString(in Scalar other, out Scalar error) {
            if (IsString && other.IsString) {
                error = default;
                return true;
            }
            error = Error($"expect two string operands. left: {this}, right: {other}");
            return false;
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
                case ScalarType.Error:
                    sb.Append(stringValue);
                    break;
            }
        }
    }

}