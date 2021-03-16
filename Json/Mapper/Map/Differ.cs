using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Differ
    {
        public  readonly    TypeCache       typeCache;

        private readonly    List<PathItem>  path  = new List<PathItem>();
        private readonly    List<Parent> parentStack = new List<Parent>();

        public Differ(TypeCache typeCache) {
            this.typeCache = typeCache;
        }

        public Diff GetDiff<T>(T left, T right) {
            parentStack.Clear();
            path.Clear();

            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var diff = mapper.Diff(this, left, right);
            if (parentStack.Count != 0)
                throw new InvalidOperationException($"Expect objectStack.Count == 0. Was: {parentStack.Count}");
            return diff;
        }
        
        public Diff AddDiff(object left, object right) {
            var itemDiff = new Diff(path, left, right);
            int parentIndex = parentStack.Count - 1;
            if (parentIndex >= 0) {
                var parent = parentStack[parentIndex];
                var parentDiff = parent.diff;
                if (parent.diff == null) {
                    parentDiff = parent.diff = new Diff(path, parent.left, parent.right);
                }
                parentDiff.items.Add(itemDiff);

                parentIndex--;
                while (parentIndex >= 0) {
                    parent = parentStack[parentIndex];
                    if (parent.diff == null) {
                        parent.diff = new Diff(path, parent.left, parent.right);
                    }
                    parent.diff.items.Add(parentDiff);
                    parentDiff = parent.diff;
                    parentIndex--;
                }
            }
            return itemDiff;
        }

        public void PushField(PropField field) {
            var item = new PathItem {
                field = field
            };
            path.Add(item);
        }
        
        public void PushElement(int index) {
            var item = new PathItem {
                index = index
            };
            path.Add(item);
        }

        public void Pop() {
            int last = path.Count - 1;
            path.RemoveAt(last);
        }


        public void CompareElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem)
        {
            PushElement(index);
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
        public Diff(List<PathItem> items, object left, object right) {
            this.path   = items.ToArray();
            this.left   = left;
            this.right  = right;
        }
            
        public  readonly    PathItem[]      path;
        public  readonly    object          left;
        public  readonly    object          right;
        public  readonly    List<Diff>      items = new List<Diff>();
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

    public struct PathItem
    {
        public PropField    field;
        public int          index;
    }
}
