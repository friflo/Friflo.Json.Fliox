using System.Collections.Generic;

namespace Friflo.Json.Mapper.Utils
{
#if UNITY_5_3_OR_NEWER
    public static class UnityExtension
    {
        public static bool TryAdd<TKey,TValue>(this Dictionary<TKey,TValue> dictionary, TKey key, TValue value) {
            if (dictionary.ContainsKey(key))
                return false;
            
            dictionary.Add(key, value);
            return true;
        }
    }
#endif

}