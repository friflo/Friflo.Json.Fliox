// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable CompareOfFloatsByEqualityOperator
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
        public                  string          ErrorMessage=> stringValue;

        private                 bool            IsString    => type == ScalarType.String;
        private                 bool            IsNumber    => type == ScalarType.Double || type == ScalarType.Long;
        private                 bool            IsDouble    => type == ScalarType.Double;
        private                 bool            IsLong      => type == ScalarType.Long;
        public                  bool            IsBool      => type == ScalarType.Bool;
        internal                bool            IsNull      => type == ScalarType.Null;
        internal                bool            IsError     => type == ScalarType.Error;
        internal                bool            IsDefined   => type >  ScalarType.Error;
        
        public                  bool            IsTrue      => type == ScalarType.Bool && primitiveValue != 0;
        public                  bool            IsFalse     => type == ScalarType.Bool && primitiveValue == 0;


        
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
        public Scalar EqualsTo(in Scalar other) {
            switch (type) {
                case ScalarType.String:
                    if (other.IsString)
                        return stringValue == other.stringValue ? True : False;
                    if (other.IsNull)
                        return Null;
                    return EqualsError(other);
                case ScalarType.Double:
                    if (other.IsDouble)
                        return DoubleValue == other.DoubleValue ? True : False;
                    if (other.IsLong)
                        return EqualsDouble(DoubleValue, other.LongValue);
                    if (other.IsNull)
                        return Null;
                    return EqualsError(other);
                case ScalarType.Long:
                    if (other.IsDouble)
                        return EqualsDouble(LongValue, other.DoubleValue);
                    if (other.IsLong)
                        return LongValue == other.LongValue ? True : False;
                    if (other.IsNull)
                        return Null;
                    return EqualsError(other);
                case ScalarType.Bool:
                    if (other.IsBool)
                        return primitiveValue == other.primitiveValue ? True : False; // possible primitive values: 0 or 1
                    if (other.IsNull)
                        return Null;
                    return EqualsError(other);
                case ScalarType.Null:
                    if (other.IsNull)
                        return True;
                    return Null;
                default:
                    throw new NotSupportedException($"Scalar does not support EqualsTo() for type: {type}");                
            }
        }
        
        private static Scalar EqualsDouble(double left, double right) {
            return left == right ? True : False;
        }
        
        private Scalar EqualsError(in Scalar other) {
            var sb = new StringBuilder();
            sb.Append("Cannot compare ");
            AppendTo(sb);
            sb.Append(" with ");
            other.AppendTo(sb);
            return Error(sb.ToString());
        }
        
        public long CompareTo(in Scalar other, Operation left, Operation right, out Scalar result) {
            switch (type) {
                case ScalarType.String:
                    if (other.IsString) {
                        result = new Scalar();
                        return string.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                    }
                    return ReturnDefault(other, left, right, out result);
                case ScalarType.Double:
                    if (other.IsDouble) {
                        result = new Scalar();
                        return CompareDouble(DoubleValue, other.DoubleValue);
                    }
                    if (other.IsLong) {
                        result = new Scalar();
                        return CompareDouble(DoubleValue, other.LongValue);
                    }
                    return ReturnDefault(other, left, right, out result);
                case ScalarType.Long:
                    if (other.IsDouble) {
                        result = new Scalar();
                        return CompareDouble(LongValue, other.DoubleValue);
                    }
                    if (other.IsLong) {
                        result = new Scalar();
                        return LongValue - other.LongValue;
                    }
                    return ReturnDefault(other, left, right, out result);
                case ScalarType.Bool:
                    if (other.IsBool) {
                        result = new Scalar();
                        return primitiveValue - other.primitiveValue; // possible primitive values: 0 or 1
                    } 
                    return ReturnDefault(other, left, right, out result);
                case ScalarType.Null:
                    if (other.IsTrue) {
                        result = new Scalar();
                        return 0;
                    } 
                    return ReturnDefault(other, left, right, out result);
                case ScalarType.Error:
                    result = this;
                    return 0;
                default:
                    throw new NotSupportedException($"Scalar does not support CompareTo() for type: {type}");                
            }
        }

        private static int CompareDouble(double left, double right) {
            double dif = left - right;
            return dif < 0 ? -1 : (dif > 0 ? +1 : 0);
        }
        
        private int ReturnDefault(in Scalar other, Operation left, Operation right, out Scalar result) {
            switch (other.type) {
                case ScalarType.Null:   result = Null;                              break;
                case ScalarType.Error:  result = other;                             break;
                default:                result = CompareError(other, left, right);  break;                
            }
            return 0;
        }
        
        private Scalar CompareError(in Scalar other, Operation left, Operation right) {
            var sb = new StringBuilder();
            sb.Append("Cannot compare ");
            AppendTo(sb);
            sb.Append(" with ");
            other.AppendTo(sb);
            if (left != null) {
                sb.Append(" left: ");
                sb.Append(left.Linq);
            }
            if (right != null) {
                sb.Append(" right: ");
                sb.Append(right.Linq);
            }
            return Error(sb.ToString());
        }
        
        // --- unary arithmetic operations ---
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
            return Error($"expect numeric operand. was: {this} in {operation.Linq}");
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