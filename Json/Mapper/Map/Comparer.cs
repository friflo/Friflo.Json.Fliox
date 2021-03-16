using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Comparer
    {
        public readonly     TypeCache       typeCache;
        private readonly    List<PathItem>  path  = new List<PathItem>();
        private readonly    List<Diff>      diffs = new List<Diff>();
        
        public void AddDiff(object left, object right) {
            int parentPos = path.Count - 1;
            path[parentPos].IncrementDiffCount();
            var diff = new Diff(path, left, right);
            diffs.Add(diff);
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


        public bool CompareElement<T> (TypeMapper<T> elementType, int index, T leftItem, T rightItem) {
            bool isEqual = true;
            PushElement(index);
            bool leftNull  = elementType.IsNull(ref leftItem);
            bool rightNull = elementType.IsNull(ref rightItem);
            if (!leftNull || !rightNull) {
                if (!leftNull && !rightNull) {
                    bool itemsEqual = elementType.Compare(this, leftItem, rightItem);
                    isEqual &= itemsEqual;
                    if (!itemsEqual)
                        AddDiff(leftItem, rightItem);
                } else {
                    isEqual = false;
                    AddDiff(leftItem, rightItem);
                }
            }
            Pop();
            return isEqual;
        }
    }

    public class Diff
    {
        public Diff(List<PathItem> items, object left, object right) {
            this.path   = items.ToArray();
            this.left   = left;
            this.right  = right;
        }
            
        public readonly     PathItem[]  path;
        public readonly     object      left;
        public readonly     object      right;
    }

    public struct PathItem
    {
        public PropField    field;
        public int          index;
        public int          diffCount;

        public void IncrementDiffCount() {
            diffCount++;
        }
    }
}
