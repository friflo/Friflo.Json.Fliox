// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    public sealed class Differ : IDisposable
    {
        private             TypeCache       typeCache;
        private             ObjectWriter    jsonWriter;
        private readonly    List<TypeNode>  path        = new List<TypeNode>();
        private readonly    List<Parent>    parentStack = new List<Parent>();
        
        public              TypeCache       TypeCache => typeCache;

        internal Differ() { }

        public void Dispose() { }

        public DiffNode GetDiff<T>(T left, T right, ObjectWriter jsonWriter) {
            this.jsonWriter = jsonWriter;
            typeCache       = jsonWriter.TypeCache;
            // --- init parentStack
            parentStack.Clear();
            var rootParent  = new Parent(left, right);
            parentStack.Add(rootParent);
            // --- init path
            path.Clear();
            var mapper      = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var rootNode    = new TypeNode(NodeType.Root, new JsonKey(), -1, mapper);
            path.Add(rootNode);
            
            mapper.Diff(this, left, right);
            
            this.jsonWriter = null;
            if (parentStack.Count != 1)
                throw new InvalidOperationException($"Expect objectStack.Count == 0. Was: {parentStack.Count}");
            Pop();
            if (path.Count != 0)
                throw new InvalidOperationException($"Expect path.Count == 0. Was: {path.Count}");
            var diff = rootParent.diff;
            if (diff == null)
                return null;
            var children = diff.children; 
            if (diff.children.Count == 0)
                return null;
            // GenericICollectionMapper<> adds the whole collection as DiffType.NotEqual additional to DiffNode
            // containing the diffs of the elements. This diff is the last one => return the last child in children.
            return children[children.Count - 1];
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
                    path[parentOfParentIndex], new Var(parent.left), new Var(parent.right), new List<DiffNode>());
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = parent.diff = new DiffNode(DiffType.None, jsonWriter, null,
                path[0], new Var(parent.left), new Var(parent.right), new List<DiffNode>());
            return parentDiff;
        }

        public DiffType AddNotEqualObject<T>(T left, T right) {
            return AddNotEqual(new Var(left), new Var(right));
        }
            
        public DiffType AddNotEqual(in Var left, in Var right) {
            AssertPathCount();
            int parentIndex = parentStack.Count - 1;
            var parent      = GetParent(parentIndex);
            var itemDiff    = new DiffNode(DiffType.NotEqual, jsonWriter, parent, path[parentIndex], left, right, null);
            parent.children.Add(itemDiff);
            return DiffType.NotEqual;
        }
        
        public DiffNode AddOnlyLeft(in Var left) {
            AssertPathCount();
            int parentIndex = parentStack.Count - 1;
            var parent      = GetParent(parentIndex);
            var itemDiff    = new DiffNode(DiffType.OnlyLeft, jsonWriter, parent, path[parentIndex], left, default, null);
            parent.children.Add(itemDiff);
            return itemDiff;
        }
        
        public DiffNode AddOnlyRight(in Var right) {
            AssertPathCount();
            int parentIndex = parentStack.Count - 1;
            var parent      = GetParent(parentIndex);
            var itemDiff    = new DiffNode(DiffType.OnlyRight, jsonWriter, parent, path[parentIndex], default, right, null);
            parent.children.Add(itemDiff);
            return itemDiff;
        }
        
        [Conditional("DEBUG")]
        private void AssertPathCount() {
            if (path.Count != parentStack.Count)
                throw new InvalidOperationException("Expect path.Count != parentStack.Count + 1");
        }

        public void PushMember(PropField field) {
            var item = new TypeNode(NodeType.Member, field.key, -1, field.fieldType);
            path.Add(item);
        }
        
        public void PushKey(TypeMapper mapper, in JsonKey key) {
            var item = new TypeNode(NodeType.Member, key, -1, mapper);
            path.Add(item);
        }
        
        public void PushElement(int index, TypeMapper elementType) {
            var item = new TypeNode(NodeType.Element, new JsonKey(), index, elementType);
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
                    AddNotEqualObject(leftItem, rightItem);
                }
            }
            Pop();
        }

        public void PushParent<T>(T left, T right) {
            parentStack.Add(new Parent(left, right));
        }
        
        public DiffType PopParent() {
            var lastIndex = parentStack.Count - 1;
            var last = parentStack[lastIndex];
            parentStack.RemoveAt(lastIndex);
            return last.diff == null ? DiffType.Equal : DiffType.NotEqual;
        }
    }

    internal sealed class Parent
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
