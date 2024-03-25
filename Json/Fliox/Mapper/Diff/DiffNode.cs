// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;


namespace Friflo.Json.Fliox.Mapper.Diff
{
    public enum DiffType
    {
        // Use continuous ids 0, 1, 2 for None, NotEqual, OnlyRight to avoid jump tables in common switch / case statements 
        None        = 0,
        Equal       = 3,
        NotEqual    = 1,
        OnlyLeft    = 4,
        OnlyRight   = 2,
    }

    public sealed class DiffNode
    {
                        public   IReadOnlyList<DiffNode>    Children    => children;
                        public              DiffType        DiffType    => diffType;
                        // --- pathNode fields
                        public              int             NodeIndex   => pathNode.index;
                        /// <summary>Either a <see cref="PropField"/> or the key of a <see cref="Dictionary{TKey,TValue}"/></summary>
                        public              object          NodeKey     => pathNode.key;
                        internal            NodeType        NodeType    => pathNode.NodeType;
        [Browse(Never)] public              TypeMapper      NodeMapper  => pathNode.mapper;
                        // --- left and right value
                        internal            Var             valueLeft;
                        internal            Var             valueRight;
        [Browse(Never)] internal            DiffType        diffType;
        // --- following private fields behave as readonly. They are mutable to enable pooling DiffNode's
                        private             DiffNode        parent; 
        [Browse(Never)] private             TypeNode        pathNode;
        [Browse(Never)] internal  readonly  List<DiffNode>  children    = new List<DiffNode>();

        
        internal DiffNode() { }
        
        internal void Init(DiffType diffType, DiffNode parent, in TypeNode pathNode, in Var left, in Var right) {
            this.diffType   = diffType;
            this.parent     = parent;
            this.pathNode   = pathNode;
            this.valueLeft  = left;
            this.valueRight = right;
            children.Clear();
        }

        public override string ToString() {
            var sb = new StringBuilder();
            CreatePath(sb, true, 0, 0);
            return sb.ToString();
        }

        internal void AddPath(StringBuilder sb) {
            CreatePath(sb, false, 0, 0);
        }
        
        private void CreatePath(StringBuilder sb, bool addValue, int startPos, int indent) {
            if (parent != null)
                parent.CreatePath(sb, false, startPos, indent);
            switch (pathNode.NodeType) {
                case NodeType.Key:
                    sb.Append('/');
                    var key = pathNode.key;
                    if (key is PropField field) {
                        sb.Append(field.name);
                    } else {
                        sb.Append(key);
                    }
                    // sb.Append(pathNode.name.AsString());
                    if (!addValue)
                        return;
                    Indent(sb, startPos, indent);
                    sb.Append(' ');
                    AddValue(sb, pathNode.mapper);
                    break;
                case NodeType.Element:
                    sb.Append('/');
                    sb.Append(pathNode.index);
                    if (!addValue)
                        return;
                    Indent(sb, startPos, indent);
                    sb.Append(' ');
                    AddValue(sb, pathNode.mapper);
                    break;
                case NodeType.Root:
                    if (!addValue)
                        return;
                    AddValue(sb, pathNode.mapper);
                    break;
            }
        }

        private static void Indent(StringBuilder sb, int startPos, int indent) {
            var pathLen = sb.Length - startPos;
            for (int i = pathLen; i < indent - 1; i++)
                sb.Append(' ');
        }

        private void AddValue(StringBuilder sb, TypeMapper mapper) {
            switch (diffType) {
                case DiffType.NotEqual:
                case DiffType.None:
                    var isComplex = mapper.IsComplex;
                    if (isComplex) {
                        AppendObject(sb, valueLeft.TryGetObject());
                        sb.Append(" != ");
                        AppendObject(sb, valueRight.TryGetObject());
                        return;
                    }
                    if (mapper.IsArray) {
                        var leftArray   = valueLeft. TryGetObject();
                        var rightArray  = valueRight.TryGetObject();
                        AppendArray(sb, mapper, leftArray);
                        sb.Append(" != ");
                        AppendArray(sb, mapper, rightArray);
                        return;
                    }
                    AppendValue(sb, valueLeft);
                    sb.Append(" != ");
                    AppendValue(sb, valueRight);
                    break;
                case DiffType.OnlyLeft:
                    AppendValue(sb, valueLeft);
                    sb.Append(" != (missing)");
                    break;
                case DiffType.OnlyRight:
                    sb.Append("(missing) != ");
                    AppendValue(sb, valueRight);
                    break;
            }
        }
        
        private static void AppendArray(StringBuilder sb, TypeMapper mapper, object array) {
            if (array == null) {
                sb.Append("null");
                return;
            }
            var count = mapper.Count(array);
            sb.Append(VarType.GetTypeName(array.GetType()));
            sb.Append("(count: ");
            sb.Append(count);
            sb.Append(')');
        }
        
        private static void AppendObject(StringBuilder sb, object value) {
            if (value == null) {
                sb.Append("null");
                return;
            }
            var type = value.GetType();
            sb.Append('{');
            sb.Append(type.Name);
            sb.Append('}');
        }

        private static void AppendValue(StringBuilder sb, in Var varValue) {
            var valueString = varValue.AsString();
            sb.Append(valueString);
        }

        public string Text => TextIndent(20);
        
        public string TextIndent(int indent) {
            var sb = new StringBuilder();
            sb.Append('\n');
            if (NodeType == NodeType.Root) {
                sb.Append('/');
                var sbLen = sb.Length;
                Indent(sb, sbLen, indent);
            }
            AppendNode(sb, indent);
            return sb.ToString();
        }
        
        private void AppendNode(StringBuilder sb, int indent) {
            var sbLen = sb.Length;
            CreatePath(sb, true, sbLen, indent);
            sb.Append('\n');
            foreach (var child in children) {
                child.AppendNode(sb, indent);
            }
        }
    }
}
