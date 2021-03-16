using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Comparer
    {
        public  readonly    TypeCache       typeCache;

        private readonly    List<PathItem>  path  = new List<PathItem>();
        private readonly    List<ObjectDiff> objectStack = new List<ObjectDiff>();

        public Comparer(TypeCache typeCache) {
            this.typeCache = typeCache;
        }

        public Diff GetDiff<T>(T left, T right) {
            objectStack.Clear();
            path.Clear();

            var mapper = (TypeMapper<T>) typeCache.GetTypeMapper(typeof(T));
            var diff = mapper.Diff(this, left, right);
            if (objectStack.Count != 0)
                throw new InvalidOperationException($"Expect objectStack.Count == 0. Was: {objectStack.Count}");
            return diff;
        }
        
        public Diff AddDiff(object left, object right) {
            int lastObjectDiff = objectStack.Count - 1;
            var array = objectStack[lastObjectDiff];
            if (array.objectDiff == null) {
                array.objectDiff = new Diff(path, array.left, array.right);
            }
            var itemDiff = new Diff(path, left, right);
            array.objectDiff.items.Add(itemDiff);
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

        public void PushObject(object left, object right) {
            objectStack.Add(new ObjectDiff(left, right));
        }
        
        public Diff PopObject() {
            var lastIndex = objectStack.Count - 1;
            var last = objectStack[lastIndex];
            objectStack.RemoveAt(lastIndex);
            return last.objectDiff;
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

    class ObjectDiff
    {
        public readonly     object      left;
        public readonly     object      right;
        public              Diff        objectDiff;

        public ObjectDiff(object left, object right) {
            this.left = left;
            this.right = right;
            objectDiff = null;
        }
    }

    public struct PathItem
    {
        public PropField    field;
        public int          index;
    }
}
