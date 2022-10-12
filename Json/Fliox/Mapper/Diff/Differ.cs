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
        private readonly    TypeCache       typeCache;
        //
        private readonly    TypeNode[]      path;
        private             int             pathIndex;
        //
        private readonly    Parent[]        parentStack; 
        private             int             parentStackIndex;
        //
        private readonly    int             maxDepth;
        private readonly    List<DiffNode>  diffNodePool = new List<DiffNode>();
        private             int             diffNodePoolIndex;
        public              TypeCache       TypeCache => typeCache;

        // JSON with with depth of 32 seems sufficient
        public Differ(TypeCache typeCache, int maxDepth = 32) {
            this.typeCache  = typeCache;
            this.maxDepth   = maxDepth;
            path            = new TypeNode  [maxDepth];
            parentStack     = new Parent    [maxDepth];
        }

        public void Dispose() { }

        public DiffNode GetDiff<T>(T left, T right) {
            diffNodePoolIndex   = 0;
            // --- init parentStack
            parentStackIndex    = 0;
            // --- init path
            var mapper          = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            path[0]             = new TypeNode(TypeNode.RootTag, -1, mapper);
            pathIndex           = 1;
            
            mapper.Diff(this, left, right);
            
            if (parentStackIndex != 0)
                throw new InvalidOperationException($"Expect parentStackIndex == 0. Was: {parentStackIndex}");
            Pop();
            path[0]         = default; // clear references
            if (pathIndex != 0)
                throw new InvalidOperationException($"Expect path.Count == 0. Was: {pathIndex}");
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
            var node = new DiffNode();
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
            if (pathIndex != parentStackIndex + 1)
                throw new InvalidOperationException("Expect path.Count != parentStackIndex + 1");
        }

        public void PushMember  (PropField field) {
            path[pathIndex++] = new TypeNode(field, -1, field.fieldType);
        }
        
        public void PushKey     (TypeMapper mapper, object key) {
            path[pathIndex++] = new TypeNode(key, -1, mapper);
        }
        
        public void PushElement (TypeMapper elementType, int index) {
            path[pathIndex++] = new TypeNode(null, index, elementType);
        }

        public void Pop() {
            path[--pathIndex] = default; // clear references
        }

        public void DiffElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem) {
            PushElement(elementType, index);
            elementType.Diff(this, leftItem, rightItem);
            Pop();
        }

        public void PushParent<T>(T left, T right) {
            if (parentStackIndex++ >= maxDepth) throw new InvalidOperationException("Exceed max depth while diffing");
            parentStack[parentStackIndex] = new Parent(left, right);
        }

        public DiffType PopParent() {
            var headDiff    = parentStack[parentStackIndex].diff;
            parentStack[parentStackIndex--] = default; // clear references
            return headDiff == null ? Equal : NotEqual;
        }
    }
}
