// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using Friflo.Json.Fliox.Mapper.Utils;
using static Friflo.Json.Fliox.Mapper.Diff.DiffType;

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
        private readonly    List<DiffNode>  diffNodePool = new List<DiffNode>();
        private             int             diffNodePoolIndex;
        public              TypeCache       TypeCache => typeCache;

        public Differ(int maxDepth = 32) {
            parentStackMaxDepth = maxDepth;
            parentStack         = new Parent[maxDepth]; // JSON with with depth of 32 seems sufficient
        }

        public void Dispose() { }

        public DiffNode GetDiff<T>(T left, T right, ObjectWriter jsonWriter) {
            this.jsonWriter     = jsonWriter;
            typeCache           = jsonWriter.TypeCache;
            diffNodePoolIndex   = 0;
            // --- init parentStack
            var rootParent      = new Parent(left, right);
            parentStack[0]      = rootParent;
            parentStackIndex    = 0;
            // --- init path
            path.Clear();
            var mapper      = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var rootNode    = new TypeNode(NodeType.Root, null, -1, mapper);
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
        
        private DiffNode CreateDiffNode() {
            if (diffNodePoolIndex < diffNodePool.Count) {
                return diffNodePool[diffNodePoolIndex++];
            }
            diffNodePoolIndex++;
            var node = new DiffNode(jsonWriter);
            diffNodePool.Add(node);
            return node;
        }

        private DiffNode GetParent(int parentIndex) {
            var parent = parentStack[parentIndex];
            var parentDiff = parent.diff;
            if (parentDiff != null)
                return parentDiff;

            var parentOfParentIndex = parentIndex - 1;
            if (parentOfParentIndex >= 0) {
                DiffNode parentOfParent = GetParent(parentOfParentIndex);
                parentDiff = CreateDiffNode();
                parentDiff.Init(None, parentOfParent, path[parentOfParentIndex], new Var(parent.left), new Var(parent.right));
                parentStack[parentIndex].diff = parentDiff;  // parent is a struct => update field in array
                parentOfParent.children.Add(parentDiff);
                return parentDiff;
            }
            parentDiff = CreateDiffNode();
            parentDiff.Init(None, null, path[0], new Var(parent.left), new Var(parent.right));
            parentStack[parentIndex].diff = parentDiff;  // parent is a struct => update field in array
            return parentDiff;
        }

        internal DiffType AddNotEqualObject<T>(T left, T right) {
            return AddNotEqual(new Var(left), new Var(right));
        }
            
        internal DiffType AddNotEqual(in Var left, in Var right) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = CreateDiffNode(); 
            itemDiff.Init(NotEqual, parent, path[parentStackIndex], left, right);
            parent.children.Add(itemDiff);
            return NotEqual;
        }
        
        internal void AddOnlyLeft(in Var left) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = CreateDiffNode(); 
            itemDiff.Init(OnlyLeft, parent, path[parentStackIndex], left, default);
            parent.children.Add(itemDiff);
        }
        
        internal void AddOnlyRight(in Var right) {
            AssertPathCount();
            var parent      = GetParent(parentStackIndex);
            var itemDiff    = CreateDiffNode(); 
            itemDiff.Init(OnlyRight, parent, path[parentStackIndex], default, right);
            parent.children.Add(itemDiff);
        }
        
        [Conditional("DEBUG")]
        private void AssertPathCount() {
            if (path.Count != parentStackIndex + 1)
                throw new InvalidOperationException("Expect path.Count != parentStackIndex + 1");
        }

        public void PushMember(PropField field) {
            var item = new TypeNode(NodeType.Member, field.name, -1, field.fieldType);
            path.Add(item);
        }
        
        public void PushKey(TypeMapper mapper, object key) {
            var item = new TypeNode(NodeType.Member, key, -1, mapper);
            path.Add(item);
        }
        
        public void PushElement(int index, TypeMapper elementType) {
            var item = new TypeNode(NodeType.Element, null, index, elementType);
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
            return headDiff == null ? Equal : NotEqual;
        }
    }

    internal struct Parent
    {
        internal  readonly  object      left;   // not required but very handy for debugging
        internal  readonly  object      right;  // not required but very handy for debugging
        internal            DiffNode    diff;

        public    override  string      ToString() => diff?.ToString();

        public Parent(object left, object right) {
            this.left   = left  ?? throw new ArgumentNullException(nameof(left));
            this.right  = right ?? throw new ArgumentNullException(nameof(right));
            diff        = null;
        }
    }
}
