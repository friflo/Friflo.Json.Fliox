// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Graph
{
    public enum DiffType
    {
        None,
        NotEqual,
        OnlyLeft,
        OnlyRight,
    }

    public class DiffNode
    {
        public  readonly    DiffType        diffType;
        public  readonly    DiffNode        parent; 
        public  readonly    PathNode        pathNode;
        public  readonly    object          left;
        public  readonly    object          right;
        public  readonly    List<DiffNode>  children;
        private readonly    JsonWriter      jsonWriter;
        
        public DiffNode(DiffType diffType, JsonWriter jsonWriter, DiffNode parent, PathNode pathNode, object left, object right, List<DiffNode> children) {
            this.diffType   = diffType;
            this.parent     = parent;
            this.pathNode   = pathNode;
            this.left       = left;
            this.right      = right;
            this.children   = children;
            this.jsonWriter = jsonWriter;
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
                    sb.Append(pathNode.name);
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
                    sb.Append(" ");
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
                sb.Append(" ");
        }

        private void AddValue(StringBuilder sb, TypeMapper mapper) {
            switch (diffType) {
                case DiffType.NotEqual:
                case DiffType.None:
                    var isComplex = mapper.IsComplex;
                    if (isComplex) {
                        AppendObject(sb, left);
                        sb.Append(" != ");
                        AppendObject(sb, right);
                        return;
                    }
                    if (mapper.IsArray) {
                        var leftCount = mapper.Count(left);
                        var rightCount = mapper.Count(right);
                        sb.Append("[");
                        AppendValue(sb, leftCount);
                        sb.Append("] != [");
                        AppendValue(sb, rightCount);
                        sb.Append("]");
                        return;
                    }
                    AppendValue(sb, left);
                    sb.Append(" != ");
                    AppendValue(sb, right);
                    break;
                case DiffType.OnlyLeft:
                    AppendValue(sb, left);
                    sb.Append(" != (missing)");
                    break;
                case DiffType.OnlyRight:
                    sb.Append("(missing) != ");
                    AppendValue(sb, right);
                    break;
            }
        }
        
        private void AppendObject(StringBuilder sb, object value) {
            if (value == null) {
                sb.Append("null");
                return;
            }
            if (pathNode.typeMapper.type == typeof(string)) {
                sb.Append(value);
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
            if (len >= 2 && str[0] == '"' && str[len - 1] == '"')
                sb.Append(str, 1, len - 2);
            else
                sb.Append(str);
        }
        
        public string GetChildrenDiff(int indent) {
            var sb = new StringBuilder();
            sb.Append((object)null);
            if (children != null) {
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

    public struct PathNode
    {
        internal    NodeType    nodeType;
        public      string      name;
        public      int         index;
        public      TypeMapper  typeMapper;
    }
}
