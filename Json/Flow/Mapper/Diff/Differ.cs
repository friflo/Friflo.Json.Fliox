// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Obj.Reflect;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Mapper.Diff
{
    public class Differ : IDisposable
    {
        public  readonly    TypeCache       typeCache;
        private readonly    ObjectWriter      jsonWriter;
        private readonly    List<TypeNode>  path        = new List<TypeNode>();
        private readonly    List<Parent>    parentStack = new List<Parent>();

        protected Differ(TypeStore typeStore) {
            this.jsonWriter = new ObjectWriter(typeStore);
            this.typeCache = jsonWriter.TypeCache;
        }
        
        public Differ(ObjectWriter jsonWriter) {
            this.jsonWriter = jsonWriter;
            this.typeCache = jsonWriter.TypeCache;
        }

        public void Dispose() {
            jsonWriter.Dispose();
        }

        public DiffNode GetDiff<T>(T left, T right) {
            parentStack.Clear();
            path.Clear();
            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var item = new TypeNode {
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

        private DiffNode GetParent(int parentIndex) {
            var parent = parentStack[parentIndex];
            var parentDiff = parent.diff;
            if (parentDiff != null)
                return parentDiff;

            var parentOfParentIndex = parentIndex - 1;
            if (parentOfParentIndex >= 0) {
                DiffNode parentOfParent = GetParent(parentOfParentIndex);
                parentDiff = parent.diff = new DiffNode(DiffType.None, jsonWriter, parentOfParent,
                    path[parentOfParentIndex + 1], parent.left, parent.right, new List<DiffNode>());
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = parent.diff = new DiffNode(DiffType.None, jsonWriter, null,
                path[0], parent.left, parent.right, new List<DiffNode>());
            return parentDiff;
        }

        public DiffNode AddNotEqual(object left, object right) {
            if (path.Count != parentStack.Count + 1)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count + 1");

            DiffNode itemDiff; 
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = GetParent(parentIndex);
                itemDiff = new DiffNode(DiffType.NotEqual, jsonWriter, parent, path[parentIndex + 1], left, right, null);
                parent.children.Add(itemDiff);
            } else {
                itemDiff = new DiffNode(DiffType.NotEqual, jsonWriter, null, path[0], left, right, null);
            }
            return itemDiff;
        }
        
        public DiffNode AddOnlyLeft(object left) {
            if (path.Count != parentStack.Count + 1)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count + 1");

            DiffNode itemDiff; 
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = GetParent(parentIndex);
                itemDiff = new DiffNode(DiffType.OnlyLeft, jsonWriter, parent, path[parentIndex + 1], left, null, null);
                parent.children.Add(itemDiff);
            } else {
                itemDiff = new DiffNode(DiffType.OnlyLeft, jsonWriter, null, path[0], left, null, null);
            }
            return itemDiff;
        }
        
        public DiffNode AddOnlyRight(object right) {
            if (path.Count != parentStack.Count + 1)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count + 1");

            DiffNode itemDiff; 
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = GetParent(parentIndex);
                itemDiff = new DiffNode(DiffType.OnlyRight, jsonWriter, parent, path[parentIndex + 1], null, right, null);
                parent.children.Add(itemDiff);
            } else {
                itemDiff = new DiffNode(DiffType.OnlyRight, jsonWriter, null, path[0], null, right, null);
            }
            return itemDiff;
        }

        public void PushMember(PropField field) {
            var item = new TypeNode {
                nodeType = NodeType.Member,
                name = field.name,
                typeMapper = field.fieldType
            };
            path.Add(item);
        }
        
        public void PushKey(TypeMapper mapper, string key) {
            var item = new TypeNode {
                nodeType = NodeType.Member,
                name = key,
                typeMapper = mapper
            };
            path.Add(item);
        }
        
        public void PushElement(int index, TypeMapper elementType) {
            var item = new TypeNode {
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
                    AddNotEqual(leftItem, rightItem);
                }
            }
            Pop();
        }

        public void PushParent(object left, object right) {
            parentStack.Add(new Parent(left, right));
        }
        
        public DiffNode PopParent() {
            var lastIndex = parentStack.Count - 1;
            var last = parentStack[lastIndex];
            parentStack.RemoveAt(lastIndex);
            return last.diff;
        }
    }

    internal class Parent
    {
        public readonly     object      left;
        public readonly     object      right;
        public              DiffNode    diff;

        public Parent(object left, object right) {
            this.left = left;
            this.right = right;
            diff = null;
        }
    }
}
