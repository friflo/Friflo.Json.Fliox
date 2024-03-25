// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Transform.Query;


// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Fliox.Transform {
    
    public readonly partial struct Scalar
    {
        // ------------------------------- compare equality of two scalars -------------------------------
        public Scalar EqualsTo(in Scalar other, Operation operation) {
            switch (type) {
                case ScalarType.String:
                    if (other.IsString)
                        return stringValue == other.stringValue ? True : False;
                    return EqualsDefault(other, operation);
                case ScalarType.Double:
                    if (other.IsDouble)
                        return DoubleValue == other.DoubleValue ? True : False;
                    if (other.IsLong)
                        return EqualsDouble(DoubleValue, other.LongValue);
                    return EqualsDefault(other, operation);
                case ScalarType.Long:
                    if (other.IsDouble)
                        return EqualsDouble(LongValue, other.DoubleValue);
                    if (other.IsLong)
                        return LongValue == other.LongValue ? True : False;
                    return EqualsDefault(other, operation);
                case ScalarType.Bool:
                    if (other.IsBool)
                        return primitiveValue == other.primitiveValue ? True : False; // possible primitive values: 0 or 1
                    return EqualsDefault(other, operation);
                case ScalarType.Null:
                    if (other.IsNull)
                        return True;
                    return False;
                case ScalarType.Object:
                case ScalarType.Array:
                    if (other.IsNull)
                        return False;
                    return CompareError("invalid operand", other, operation);
                case ScalarType.Error:
                    return this;
                default:
                    throw new NotSupportedException($"Scalar does not support EqualsTo() for type: {type}");                
            }
        }
        
        private static Scalar EqualsDouble(double left, double right) {
            return left == right ? True : False;
        }
        
        private Scalar EqualsDefault(in Scalar other, Operation operation) {
            switch (other.type) {
                case ScalarType.Null:
                    return type == ScalarType.Null ? True : False;
                case ScalarType.Array:
                case ScalarType.Object:
                    return CompareError("invalid operand", other, operation);
                default:
                    return CompareError("incompatible operands", other, operation);
            }
        }

        // ------------------------------- compare order of two scalars -------------------------------
        public long CompareTo(in Scalar other, Operation operation, out Scalar result) {
            switch (type) {
                case ScalarType.String:
                    if (other.IsString) {
                        result = default;
                        return string.CompareOrdinal(stringValue, other.stringValue);
                    }
                    return CompareDefault(other, operation, out result);
                case ScalarType.Double:
                    if (other.IsDouble) {
                        result = default;
                        return CompareDouble(DoubleValue, other.DoubleValue);
                    }
                    if (other.IsLong) {
                        result = default;
                        return CompareDouble(DoubleValue, other.LongValue);
                    }
                    return CompareDefault(other, operation, out result);
                case ScalarType.Long:
                    if (other.IsDouble) {
                        result = default;
                        return CompareDouble(LongValue, other.DoubleValue);
                    }
                    if (other.IsLong) {
                        result = default;
                        return LongValue - other.LongValue;
                    }
                    return CompareDefault(other, operation, out result);
                case ScalarType.Bool:
                    if (other.IsBool) {
                        result = default;
                        return primitiveValue - other.primitiveValue; // possible primitive values: 0 or 1
                    }
                    return CompareDefault(other, operation, out result);
                case ScalarType.Null:
                    if (other.IsNull) {
                        result = default;
                        return 0;
                    }
                    result = Null;
                    return 0;
                case ScalarType.Object:
                case ScalarType.Array:
                    result = CompareError("invalid operand", other, operation);
                    return 0;
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
        
        private int CompareDefault(in Scalar other, Operation operation, out Scalar result) {
            switch (other.type) {
                case ScalarType.Null:
                    result = Null;
                    return 0;
                case ScalarType.Array:
                case ScalarType.Object:
                    result = CompareError("invalid operand", other, operation);
                    return 0;
                case ScalarType.Error:
                    result = other;
                    return 0;
                default:
                    result = CompareError("incompatible operands", other, operation);                
                    return 0;
            }
        }
        
        private Scalar CompareError(string error, in Scalar other, Operation operation) {
            var sb = new StringBuilder();
            sb.Append(error);
            sb.Append(": ");
            AppendTo(sb);
            if (operation != null) {
                sb.Append(' ');
                sb.Append(operation.OperationName);
                sb.Append(' ');
            } else {
                sb.Append(" with ");
            }
            other.AppendTo(sb);
            if (operation != null) {
                var appendCx = new AppendCx(sb);
                sb.Append(" in ");
                operation.AppendLinq(appendCx);
            }
            return Error(sb.ToString());
        }
    }
}