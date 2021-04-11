// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Mapper.Graph
{
    public class SelectorResult
    {
        public  readonly    List<Scalar>    values = new List<Scalar>();

        internal SelectorResult() { }

        public List<string> AsStringList() {
            var result = new List<string>(values.Count);
            foreach (var item in values) {
                result.Add(item.stringValue);
            }
            return result;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AppendItemAsString(sb);
            return sb.ToString();
        }

        /// Format as debug string - not as JSON 
        private void AppendItemAsString(StringBuilder sb) {
            switch (values.Count) {
                case 0:
                    sb.Append("[]");
                    break;
                case 1:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    sb.Append(']');
                    break;
                default:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    for (int n = 1; n < values.Count; n++) {
                        sb.Append(',');
                        values[n].AppendTo(sb);
                    }
                    sb.Append(']');
                    break;
            }
        }
    }

    /// Note: Could be a readonly struct, but performance degrades and API gets unhandy if so.
    public class Scalar
    {
        internal    readonly    ResultType      type;
        internal    readonly    string          stringValue; 
        internal    readonly    bool            isFloat;
        internal    readonly    double          doubleValue;
        internal    readonly    long            longValue;
        internal    readonly    bool            boolValue;
        

        public Scalar(ResultType type, string value) {
            this.type   = type;
            stringValue = value;
        }
        
        public Scalar(string value) {
            type        = ResultType.String;
            stringValue = value;
        }
        
        public Scalar(double value) {
            type        = ResultType.Number;
            isFloat     = true;
            doubleValue = value;
        }
        
        public Scalar(long value) {
            type        = ResultType.Number;
            isFloat     = false;
            longValue   = value;
        }

        public Scalar(bool value) {
            type        = ResultType.Bool;
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
                case ResultType.String:
                    return String.Compare(stringValue, other.stringValue, StringComparison.Ordinal);
                case ResultType.Number:
                    if (isFloat) {
                        if (other.isFloat)
                            return (long) (doubleValue - other.doubleValue);
                        return (long) (doubleValue - other.longValue);
                    }
                    if (other.isFloat)
                        return (long) (longValue - other.doubleValue);
                    return longValue - other.longValue;
                case ResultType.Bool:
                    long b1 = boolValue ? 1 : 0;
                    long b2 = other.boolValue ? 1 : 0;
                    return b1 - b2;
                case ResultType.Null:
                    return 0;
                default:
                    throw new NotSupportedException($"SelectorValue does not support Compare for: {type}");                
            }
        }

        public object AsObject() {
            switch (type) {
                case ResultType.Number:
                    if (isFloat)
                        return doubleValue;
                    return longValue;
                case ResultType.String:
                    return stringValue;
                case ResultType.Bool:
                    return boolValue;
                case ResultType.Null:
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
            if (type != ResultType.Number)
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
            if (type != ResultType.Number || other.type != ResultType.Number)
                throw new InvalidOperationException($"Expect both operands being numeric. left: {this}, right: {other}");
        }
        
        // --------

        /// Format as debug string - not as JSON
        internal void AppendTo(StringBuilder sb) {
            switch (type) {
                case ResultType.Array:
                case ResultType.Object:
                    sb.Append(stringValue);
                    break;
                case ResultType.Number:
                    if (isFloat)
                        sb.Append(doubleValue);
                    else
                        sb.Append(longValue);
                    break;
                case ResultType.String:
                    sb.Append('\'');
                    sb.Append(stringValue);
                    sb.Append('\'');
                    break;
                case ResultType.Bool:
                    sb.Append(boolValue ? "true": "false");
                    break;
                case ResultType.Null:
                    sb.Append("null");
                    break;
            }
        }
    }

    public enum ResultType {
        String,
        Number,
        Bool,
        Null,
        Array,
        Object
    }
    
    public class JsonSelectorQuery : PathNodeTree<SelectorResult>
    {
        internal JsonSelectorQuery() { }
        
        public JsonSelectorQuery(IList<string> pathList) {
            CreateNodeTree(pathList);
        }

        internal new void CreateNodeTree(IList<string> pathList) {
            base.CreateNodeTree(pathList);
            foreach (var leaf in leafNodes) {
                leaf.node.result = new SelectorResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var leaf in leafNodes) {
                leaf.node.result.values.Clear();
            }
        }

        public List<SelectorResult> GetResult() {
            var results = new List<SelectorResult>(leafNodes.Count);
            foreach (var leaf in leafNodes) {
                results.Add(leaf.node.result);
            }
            return results;
        }
    }
}