// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Friflo.Json.Mapper.Graph;

namespace Friflo.Json.EntityGraph.Filter
{
    
    public abstract class GraphOp
    {
        internal virtual void Init(GraphOpContext cx) { }

        internal virtual List<SelectorValue> Eval() {
            throw new NotImplementedException($"Eval() not implemented for: {GetType().Name}");
        }
        
        internal static readonly SelectorValue True  = new SelectorValue(true); 
        internal static readonly SelectorValue False = new SelectorValue(false);
        
        internal static readonly List<SelectorValue> SingleTrue  = new List<SelectorValue>{ True  };
        internal static readonly List<SelectorValue> SingleFalse = new List<SelectorValue>{ False };
    }
    
    internal class GraphOpContext
    {
        internal readonly Dictionary<string, Field> selectors = new Dictionary<string, Field>();
    }
    
    // ------------------------------------- unary operators -------------------------------------
    public class Field : GraphOp
    {
        public          string                  field;
        public          List<SelectorValue>     values = new List<SelectorValue>();

        public override string                  ToString() => field;
        
        public Field(string field) { this.field = field; }

        internal override void Init(GraphOpContext cx) {
            cx.selectors.TryAdd(field, this);
        }

        internal override List<SelectorValue> Eval() { return values; }
    }
    
    public class StringLiteral : GraphOp
    {
        public              string      value;
        
        public override     string      ToString() => $"\"{value}\"";

        public StringLiteral(string value) { this.value = value; }

        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }
    
    public class NumberLiteral : GraphOp
    {
        public              double      value;  // or long

        public override     string      ToString() => value.ToString(CultureInfo.InvariantCulture);
        
        public NumberLiteral(double value) { this.value = value; }
        
        internal override List<SelectorValue> Eval() {
            return new List<SelectorValue> { new SelectorValue(value) };
        }
    }
    
    public class BooleanLiteral : GraphOp
    {
        public bool         value;
    }

    public class NullLiteral : GraphOp
    {
    }
    
    public class NotOp : GraphOp
    {
        public BoolOp       lambda;
    }
    
    
    // ------------------------------------- BinaryResult -------------------------------------
    internal readonly struct ResultPair {
        internal readonly SelectorValue left;
        internal readonly SelectorValue right;

        internal ResultPair(SelectorValue left, SelectorValue right) {
            this.left  = left;
            this.right = right;
        }
    }
    
    internal struct BinaryResultEnumerator : IEnumerator<ResultPair>
    {
        private readonly    SelectorValue       singleLeft;
        private readonly    SelectorValue       singleRight;
        private readonly    List<SelectorValue> left;
        private readonly    List<SelectorValue> right;
        private readonly    int                 last;
        private             int                 pos;
        
        internal BinaryResultEnumerator(BinaryResult binaryResult) {
            left  = binaryResult.left;
            right = binaryResult.right;
            singleLeft  = left. Count == 1 ? left [0] : null;
            singleRight = right.Count == 1 ? right[0] : null;
            last = Math.Max(left.Count, right.Count) - 1;
            pos = -1;
        }
        
        public bool MoveNext() {
            if (pos == last)
                return false;
            pos++;
            return true;
        }

        public void Reset() { pos = -1; }

        public ResultPair Current {
            get {
                var leftResult  = singleLeft  ?? left [pos];
                var rightResult = singleRight ?? right[pos];
                return new ResultPair(leftResult, rightResult);
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }
    } 
    
    internal readonly struct  BinaryResult : IEnumerable<ResultPair>
    {
        internal  readonly List<SelectorValue>   left;
        internal  readonly List<SelectorValue>   right;

        internal BinaryResult(List<SelectorValue> left, List<SelectorValue> right) {
            this.left  = left;
            this.right = right;
            if (left.Count == 1 || right.Count == 1)
                return;
            throw new InvalidOperationException("Expect at least an operation result with one element");
        }

        public IEnumerator<ResultPair> GetEnumerator() {
            return new BinaryResultEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }



}
