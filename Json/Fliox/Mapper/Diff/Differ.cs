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
        private readonly    Parent[]        parentStack; 
        private             int             parentStackIndex;
        private readonly    int             parentStackMaxDepth;

        public              TypeCache       TypeCache => typeCache;

        internal Differ(int maxDepth = 32) {
            parentStackMaxDepth = maxDepth;
            parentStack         = new Parent[maxDepth]; // JSON with with depth of 32 seems sufficient
        }

        public void Dispose() { }

        public DiffNode GetDiff<T>(T left, T right, ObjectWriter jsonWriter) {
            this.jsonWriter = jsonWriter;
            typeCache       = jsonWriter.TypeCache;
            // --- init parentStack
            var rootParent  = new Parent(left, right);
            parentStack[0]  = rootParent;
            parentStackIndex= 0;
            // --- init path
            path.Clear();
            var mapper      = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var rootNode    = new TypeNode(NodeType.Root, new JsonKey(), -1, mapper);
            path.Add(rootNode);
            
            mapper.Diff(this, left, right);
            
            this.jsonWriter = null;
            if (parentStackIndex != 0)
                throw new InvalidOperationException($"Expect parentStackIndex == 0. Was: {parentStackIndex}");
            Pop();
            if (path.Count != 0)
                throw new InvalidOperationException($"Expect path.Count == 0. Was: {path.Count}");
            var diff        = parentStack[0].diff; // parent is a struct => list entry is replaced subsequently
            parentStack[0]  = default; // clear references
            if (diff == null)
                return null;
            var children    = diff.children; 
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
                parentStack[parentIndex] = parent;  // parent is a struct => need to replace list entry to store diff
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = parent.diff = new DiffNode(DiffType.None, jsonWriter, null,
                path[0], new Var(parent.left), new Var(parent.right), new List<DiffNode>());
            parentStack[parentIndex] = parent;  // parent is a struct => need to replace list entry to store diff
            return parentDiff;
        }

        public DiffType AddNotEqualObject<T>(T left, T right) {
            return AddNotEqual(new Var(left), new Var(right));
        }
            
        public DiffType AddNotEqual(in Var left, in Var right) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = new DiffNode(DiffType.NotEqual, jsonWriter, parent, path[parentStackIndex], left, right, null);
            parent.children.Add(itemDiff);
            return DiffType.NotEqual;
        }
        
        public void AddOnlyLeft(in Var left) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = new DiffNode(DiffType.OnlyLeft, jsonWriter, parent, path[parentStackIndex], left, default, null);
            parent.children.Add(itemDiff);
        }
        
        public void AddOnlyRight(in Var right) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = new DiffNode(DiffType.OnlyRight, jsonWriter, parent, path[parentStackIndex], default, right, null);
            parent.children.Add(itemDiff);
        }
        
        [Conditional("DEBUG")]
        private void AssertPathCount() {
            if (path.Count != parentStackIndex - 1)
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

        public void DiffElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem) {
            PushElement(index, elementType);
            elementType.Diff(this, leftItem, rightItem);
            Pop();
        }

        public void PushParent<T>(T left, T right) {
            if (parentStackIndex++ >= parentStackMaxDepth) throw new InvalidOperationException("Exceed max depth while diffing");
            parentStack[parentStackIndex] = new Parent(left, right);
        }

        public DiffType PopParent() {
            var headDiff    = parentStack[parentStackIndex].diff;
            parentStack[parentStackIndex--] = default; // clear references
            return headDiff == null ? DiffType.Equal : DiffType.NotEqual;
        }
    }

    internal struct Parent
    {
        internal  readonly  object      left;
        internal  readonly  object      right;
        internal            DiffNode    diff;

        public    override  string      ToString() => diff?.ToString();

        public Parent(object left, object right) {
            this.left   = left  ?? throw new ArgumentNullException(nameof(left));
            this.right  = right ?? throw new ArgumentNullException(nameof(right));
            diff        = null;
        }
    }
}
