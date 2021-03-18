using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Differ : IDisposable
    {
        public  readonly    TypeCache       typeCache;

        private readonly    JsonWriter      jsonWriter;
        private readonly    List<PathNode>  path        = new List<PathNode>();
        private readonly    List<Parent>    parentStack = new List<Parent>();

        public Differ(TypeStore typeStore) {
            this.jsonWriter = new JsonWriter(typeStore);
            this.typeCache = jsonWriter.TypeCache;
        }

        public void Dispose() {
            jsonWriter.Dispose();
        }

        public Diff GetDiff<T>(T left, T right) {
            parentStack.Clear();
            path.Clear();
            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var item = new PathNode {
                nodeType = NodeType.Root,
                typeMapper = mapper 
            };
            path.Add(item);
            var diff = mapper.Diff(this, left, right);
            Pop();
            if (parentStack.Count != 0)
                throw new InvalidOperationException($"Expect objectStack.Count == 0. Was: {parentStack.Count}");
            if (path.Count != 0)
                throw new InvalidOperationException($"Expect path.Count == 0. Was: {path.Count}");
            return diff;
        }

        private Diff GetParent(int parentIndex) {
            var parent = parentStack[parentIndex];
            var parentDiff = parent.diff;
            if (parentDiff != null)
                return parentDiff;

            var parentOfParentIndex = parentIndex - 1;
            if (parentOfParentIndex >= 0) {
                Diff parentOfParent = GetParent(parentOfParentIndex);
                parentDiff = parent.diff = new Diff(jsonWriter, parentOfParent, path[parentOfParentIndex + 1], parent.left, parent.right, new List<Diff>());
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = parent.diff = new Diff(jsonWriter, null, path[0], parent.left, parent.right, new List<Diff>());
            return parentDiff;
        }

        public Diff AddDiff(object left, object right) {
            if (path.Count != parentStack.Count + 1)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count + 1");

            Diff itemDiff; 
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = GetParent(parentIndex);
                itemDiff = new Diff(jsonWriter, parent, path[parentIndex + 1], left, right, null);
                parent.children.Add(itemDiff);
            } else {
                itemDiff = new Diff(jsonWriter, null, path[0], left, right, null);
            }
            return itemDiff;
        }

        public void PushField(PropField field) {
            var item = new PathNode {
                nodeType = NodeType.Member,
                field = field,
                typeMapper = field.fieldType
            };
            path.Add(item);
        }
        
        public void PushElement(int index, TypeMapper elementType) {
            var item = new PathNode {
                nodeType = NodeType.Element,
                index = index,
                typeMapper = elementType
            };
            path.Add(item);
        }

        public void Pop() {
            int last = path.Count - 1;
            path.RemoveAt(last);
        }


        public void CompareElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem)
        {
            PushElement(index, elementType);
            bool leftNull  = elementType.IsNull(ref leftItem);
            bool rightNull = elementType.IsNull(ref rightItem);
            if (!leftNull || !rightNull) {
                if (!leftNull && !rightNull) {
                    elementType.Diff(this, leftItem, rightItem);
                } else {
                    AddDiff(leftItem, rightItem);
                }
            }
            Pop();
        }

        public void PushParent(object left, object right) {
            parentStack.Add(new Parent(left, right));
        }
        
        public Diff PopParent() {
            var lastIndex = parentStack.Count - 1;
            var last = parentStack[lastIndex];
            parentStack.RemoveAt(lastIndex);
            return last.diff;
        } 

    }


    public class Diff
    {
        public Diff(JsonWriter jsonWriter, Diff parent, PathNode pathNode, object left, object right,  List<Diff> children) {
            this.parent     = parent;
            this.pathNode   = pathNode;
            this.left       = left;
            this.right      = right;
            this.children   = children;
            this.jsonWriter = jsonWriter;
        }

        public  readonly    Diff            parent; 
        public  readonly    PathNode        pathNode;
        public  readonly    object          left;
        public  readonly    object          right;
        public  readonly    List<Diff>      children;
        private readonly    JsonWriter      jsonWriter;

        public override string ToString() {
            var sb = new StringBuilder();
            CreatePath(sb, true, 0, 0);
            return sb.ToString();
        }
        
        private void CreatePath(StringBuilder sb, bool addValue, int startPos, int indent) {
            if (parent != null)
                parent.CreatePath(sb, false, startPos, indent);
            switch (pathNode.nodeType) {
                case NodeType.Member:
                    sb.Append('/');
                    sb.Append(pathNode.field.name);
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

        private void AddValue(StringBuilder sb, TypeMapper  mapper) {
            Type        type = mapper.type;
            Type        ut   = mapper.nullableUnderlyingType;
            bool isNullablePrimitive = ut != null && ut.IsPrimitive;
            bool isNullableEnum      = ut != null && ut.IsEnum;

            bool isScalar = type.IsEnum || type.IsPrimitive || isNullablePrimitive || isNullableEnum;
            if (isScalar) {
                AppendValue(sb, left);
                sb.Append(" -> ");
                AppendValue(sb, right);
            } else {
                AppendObject(sb, left);
                sb.Append(" -> ");
                AppendObject(sb, right);
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

    class Parent
    {
        public readonly     object      left;
        public readonly     object      right;
        public              Diff        diff;

        public Parent(object left, object right) {
            this.left = left;
            this.right = right;
            diff = null;
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
        public      PropField   field;
        public      int         index;
        public      TypeMapper  typeMapper;
    }
}
