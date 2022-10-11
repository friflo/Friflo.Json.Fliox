// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;


namespace Friflo.Json.Fliox.Mapper.Diff
{
    public enum DiffType
    {
        None,
        Equal,
        NotEqual,
        OnlyLeft,
        OnlyRight,
    }

    public sealed class DiffNode
    {
                        public   IReadOnlyList<DiffNode>    Children    => children;
                        internal            DiffType        DiffType    => diffType;
                        // ReSharper disable once UnusedMember.Global
                        internal            Var             Left        => left;
                        internal            Var             Right       => right;
        // --- following private fields behave as readonly. They are mutable to enable pooling DiffNode's
        [Browse(Never)] private             DiffType        diffType;
                        private             DiffNode        parent; 
                        private             TypeNode        pathNode;
        [Browse(Never)] private             Var             left;
        [Browse(Never)] private             Var             right;
        [Browse(Never)] internal  readonly  List<DiffNode>  children    = new List<DiffNode>();
        [Browse(Never)] private   readonly  ObjectWriter    jsonWriter;
        
        internal DiffNode(ObjectWriter jsonWriter) {
            this.jsonWriter = jsonWriter;
        }
        
        internal void Init(DiffType diffType, DiffNode parent, in TypeNode pathNode, in Var left, in Var right) {
            this.diffType   = diffType;
            this.parent     = parent;
            this.pathNode   = pathNode;
            this.left       = left;
            this.right      = right;
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
            switch (pathNode.nodeType) {
                case NodeType.Member:
                    sb.Append('/');
                    sb.Append(pathNode.key);
                    // sb.Append(pathNode.name.AsString());
                    if (!addValue)
                        return;
                    Indent(sb, startPos, indent);
                    sb.Append(" ");
                    AddValue(sb, pathNode.typeMapper);
                    break;
                case NodeType.Element:
                    sb.Append('/');
                    sb.Append(pathNode.index);
                    if (!addValue)
                        return;
                    Indent(sb, startPos, indent);
                    sb.Append(' ');
                    AddValue(sb, pathNode.typeMapper);
                    break;
                case NodeType.Root:
                    if (!addValue)
                        return;
                    AddValue(sb, pathNode.typeMapper);
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
                        AppendObject(sb, left.TryGetObject());
                        sb.Append(" != ");
                        AppendObject(sb, right.TryGetObject());
                        return;
                    }
                    if (mapper.IsArray) {
                        var leftCount  = mapper.Count(left. TryGetObject());
                        var rightCount = mapper.Count(right.TryGetObject());
                        sb.Append('[');
                        AppendValue(sb, leftCount);
                        sb.Append("] != [");
                        AppendValue(sb, rightCount);
                        sb.Append(']');
                        return;
                    }
                    AppendValue(sb, left.ToObject());
                    sb.Append(" != ");
                    AppendValue(sb, right.ToObject());
                    break;
                case DiffType.OnlyLeft:
                    AppendValue(sb, left.ToObject());
                    sb.Append(" != (missing)");
                    break;
                case DiffType.OnlyRight:
                    sb.Append("(missing) != ");
                    AppendValue(sb, right.ToObject());
                    break;
            }
        }
        
        private static void AppendObject(StringBuilder sb, object value) {
            if (value == null) {
                sb.Append("null");
                return;
            }
            sb.Append("(object)");
        }

        private void AppendValue(StringBuilder sb, object value) {
            if (value == null) {
                sb.Append("null");
                return;
            }
            var str = jsonWriter.WriteObject(value);
            var len = str.Length;
            if (len >= 2 && str[0] == '"' && str[len - 1] == '"') {
                sb.Append('\'');
                sb.Append(str, 1, len - 2);
                sb.Append('\'');
            } else {
                sb.Append(str);
            }
        }
        
        public string AsString(int indent) {
            var sb = new StringBuilder();
            sb.Append((object)null);

            if (diffType == DiffType.None) {
                foreach (var child in children) {
                    child.CreatePath(sb, true, sb.Length, indent);
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
    }
    
    internal enum NodeType
    {
        Root,
        Element,
        Member,
    }

    internal readonly struct TypeNode
    {
                        public   readonly   NodeType    nodeType;
        /// <summary>Commonly the a property or field name (string). In case of a dictionary a dictionary key</summary>
                        public   readonly   object      key;
                        public   readonly   int         index;
        [Browse(Never)] public   readonly   TypeMapper  typeMapper;

                        public   override   string      ToString() => GetString();

        internal TypeNode(NodeType nodeType, object key, int index, TypeMapper typeMapper) {
            this.nodeType   = nodeType;
            this.key        = key;
            this.index      = index;
            this.typeMapper = typeMapper;
        }
        
        private string GetString() {
            switch (nodeType) {
                case NodeType.Root:     return "(Root)";
                case NodeType.Member:   return key.ToString();
                case NodeType.Element:  return $"[{index}]";
                default:    throw new InvalidOperationException("unexpected case");
            }
        }
    }
}
