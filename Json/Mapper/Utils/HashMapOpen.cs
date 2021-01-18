// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Utils
{
    /// <summary>
    /// Requirement/feature: Allocation free hash map when using Get()
    /// </summary>
    public class HashMapOpen <K,V>  /* FFMap<K,V> */ where K : struct, IMapKey<K>
    {
        private     K[]     key;
        private     V[]     val;
        private     int[]   used;
        private     int     capacity;
        private     int     size;
        private     int     threshold;
        private     K       removedKey;
        private     int     removes;
        private     int     thresholdRemoves;

        public HashMapOpen(int capacity, K removedKey)
        {
            key =   new K       [ capacity ];
            val =   new V       [ capacity ];
            used =  new int     [capacity];
            //
            this.capacity = capacity;
            threshold = (int)(0.7 * capacity);
            thresholdRemoves    = (int)(0.15 * capacity);
            this.removedKey     = removedKey;
        }

        public int Size()
        {
            return size;
        }

        public bool Remove (ref K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            ref K e = ref key[idx];
            while (e.IsSet())
            {
                if (e. IsEqual( ref k ))
                {
                    key[idx] = removedKey;
                    val[idx] = default(V);
                    if (removes++ >= thresholdRemoves )
                        Rehash(capacity);
                    return true;
                }
                idx = (idx + 1) % capacity;
                e = ref key[idx];
            }
            return false;
        }

        public void Put(ref K k, V v)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            // NOTE: check for rehashing slow down performance by factor 2.5
            if (size >= threshold)
                Rehash ( 2 * capacity + 1);
            int idx = hash % capacity;
            ref K e = ref key[idx];
            while (e.IsSet())
            {
                if (e. IsEqual( ref k ))
                {
                    val[idx] = v;
                    return;
                }
                idx = (idx + 1) % capacity;
                e = ref key[idx];
            }

            key[idx] = k;
            val[idx] = v;
            used[size++] = idx;     
        }
    
        public V Get (ref K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            ref K e = ref key[idx];
            while (e.IsSet())
            {
                if (e. IsEqual(ref k ))
                    return val[idx];
                idx = (idx + 1) % capacity;
                e = ref key[idx];           
            }
            return default(V);
        }
    
        public bool ContainsKey (ref K k)
        {
            // does not support null key (same as Dictionary)
            int hash = k. GetHashCode() & 0x7FFFFFFF;
            int idx = hash % capacity;
            ref K e = ref key[idx];
            while (e.IsSet())
            {
                if (e. IsEqual( ref k ))
                    return true;
                idx = (idx + 1) % capacity;
                e = ref key[idx];           
            }
            return false;
        }

        public void Rehash (int newCap)
        {
            capacity = newCap;
            threshold           = (int)(0.7  * capacity);
            thresholdRemoves    = (int)(0.15 * capacity);
            
            K[]         newKey  = new K    [capacity];
            V[]         newVal  = new V    [capacity];
            int[]       newUsed = new int  [capacity];
            int         newSize = 0;
        
            for (int n = 0; n < size; n++)
            {
                int pos = used[n];
                ref K k = ref key[pos];
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (!k.IsEqual(ref removedKey))
                {
                    int hash = k. GetHashCode() & 0x7FFFFFFF;
                    int idx = hash % capacity;
                    ref K e = ref newKey[idx];
                    while (e.IsSet())
                    {
                        idx = (idx + 1) % capacity;
                        e = ref newKey[idx];
                    }
                    newKey[idx] = k;
                    newVal[idx] = val[pos];
                    newUsed[newSize++] = idx;
                }
                key[pos]= default;
                val[pos]= default(V);
            }

            key = newKey;
            val = newVal;
            used = newUsed;
            size = newSize;
            removes = 0;
        }

        public void Clear()
        {
            for (int n = 0; n < size; n++)
                key[used[n]] = default(K);
            size = 0;
        }


    }
}