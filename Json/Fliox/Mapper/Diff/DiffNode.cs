// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Val;
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
                        var leftCount   = mapper.Count(leftArray);
                        var rightCount  = mapper.Count(rightArray);
                        sb.Append(GetTypeName(leftArray.GetType()));
                        sb.Append("(count: ");
                        sb.Append(leftCount);
                        sb.Append(')');
                        sb.Append(" != ");
                        
                        sb.Append(GetTypeName(rightArray.GetType()));
                        sb.Append("(count: ");
                        sb.Append(rightCount);
                        sb.Append(')');
                        return;
                    }
                    AppendValue(sb, valueLeft.ToObject());
                    sb.Append(" != ");
                    AppendValue(sb, valueRight.ToObject());
                    break;
                case DiffType.OnlyLeft:
                    AppendValue(sb, valueLeft.ToObject());
                    sb.Append(" != (missing)");
                    break;
                case DiffType.OnlyRight:
                    sb.Append("(missing) != ");
                    AppendValue(sb, valueRight.ToObject());
                    break;
            }
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

        private static StringBuilder AppendValue(StringBuilder sb, object value) {
            switch (value) {
                case null:                  return sb.Append("null");
                case string     str:        sb.Append('\''); sb.Append(str); sb.Append('\'');   return sb;
                case DateTime   dateTime:   return sb.Append(DateTimeMapper.ToRFC_3339(dateTime));
                case bool       b:          return sb.Append(b ? "true" : "false");
                case char       c:          sb.Append('\''); sb.Append(c); sb.Append('\'');     return sb;
            }
            var type = value.GetType();
            if (type == typeof(DateTime?))  return sb.Append(DateTimeMapper.ToRFC_3339(((DateTime?)value).Value));
            if (type == typeof(bool?))      return sb.Append(((bool?)value).Value ? "true" : "false");
            if (type == typeof(char?))      { sb.Append('\''); sb.Append(((char?)value).Value); sb.Append('\''); return sb; }

            return sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
        }

        public string AsString(int indent) {
            var sb = new StringBuilder();
            AppendNode(sb, indent);
            return sb.ToString();
        }
        
        private void AppendNode(StringBuilder sb, int indent) {
            if (diffType == DiffType.None) {
                foreach (var child in children) {
                    child.CreatePath(sb, true, sb.Length, indent);
                    sb.Append('\n');
                    child.AppendNode(sb, indent);
                }
            }
        }
        
        private static string GetTypeName(Type type) {
            if (type.IsGenericType) {
                var genericArgs = type.GetGenericArguments().Select(GetTypeName);
                var idx         = type.Name.IndexOf('`');
                var typename    = (idx > 0) ? type.Name.Substring(0, idx) : type.Name;
                var args        = string.Join(", ", genericArgs);
                return $"{typename}<{args}>";
            }
            if (type.IsArray) {
                return GetTypeName(type.GetElementType()) + "[]";
            }
            return type.Name;
        }
    }
}
