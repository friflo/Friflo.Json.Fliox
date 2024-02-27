// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Fliox.Transform.Tree;

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Json.Fliox.Transform
{
    public enum ScalarType : byte {
        Undefined   = 0,
        Error       = 1,
        //
        String      = 2,
        Double      = 3,
        Long        = 4,
        Null        = 5, // enhance case performance by putting it directly to Double & Long
        Bool        = 6,
        Array       = 7,
        Object      = 8
    }

    public readonly partial struct Scalar
    {
        public      readonly    ScalarType      type;           // 1 byte - underlying type set to byte
        private     readonly    long            primitiveValue; // 8 bytes: union: long | double | bool | firstAstChild
        private     readonly    string          stringValue;    // 8 bytes

        private                 double          DoubleValue => BitConverter.Int64BitsToDouble(primitiveValue);
        private                 long            LongValue   => primitiveValue;
        private                 bool            BoolValue   => primitiveValue != 0;
        public                  string          ErrorMessage=> stringValue;

        private                 bool            IsString    => type == ScalarType.String;
        private                 bool            IsNumber    => type == ScalarType.Double || type == ScalarType.Long;
        private                 bool            IsDouble    => type == ScalarType.Double;
        private                 bool            IsLong      => type == ScalarType.Long;
        private                 bool            IsBool      => type == ScalarType.Bool;
        public                  bool            IsNull      => type == ScalarType.Null;
        internal                bool            IsError     => type == ScalarType.Error;
        
        public                  bool            IsTrue      => type == ScalarType.Bool && primitiveValue != 0;
        public                  bool            IsFalse     => type == ScalarType.Bool && primitiveValue == 0;
        

        public static readonly  Scalar          True    = new Scalar(true); 
        public static readonly  Scalar          False   = new Scalar(false);
        
        public static readonly  Scalar          Zero    = new Scalar(0); 

        // ReSharper disable once InconsistentNaming
        public static readonly  Scalar          PI      = new Scalar(Math.PI);
        public static readonly  Scalar          E       = new Scalar(Math.E);
        public static readonly  Scalar          Tau     = new Scalar(6.2831853071795862); // Math.Tau
        
        public static readonly  Scalar          Null    = new Scalar(ScalarType.Null, null);


        private Scalar(ScalarType type, string value) {
            this.type       = type;
            stringValue     = value;
            //
            primitiveValue  = 0;
        }
        
        internal Scalar(ScalarType type, string value, int firstAstChild) {
            this.type       = type;
            stringValue     = value;
            //
            primitiveValue  = firstAstChild;
        }
        
        private static Scalar Error(string message) {
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
        
        /// <summary>
        /// Return the index of an <see cref="ScalarType.Array"/> or <see cref="ScalarType.Object"/> node in a <see cref="JsonAst"/>.<br/>
        /// To access node data use <see cref="JsonAst.Nodes"/> or <see cref="JsonAst.GetNodeValue(int)"/> 
        /// </summary>
        public int GetFirstAstChild() {
            if (type == ScalarType.Array || type == ScalarType.Object)
                return (int)primitiveValue;
            throw new InvalidOperationException($"ast child expect type Array or Object. was: {type}, value: {this}");
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
        
        /// Format as debug string - not as JSON
        private void AppendTo(StringBuilder sb) {
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