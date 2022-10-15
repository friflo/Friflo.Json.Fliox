using System.Collections.Generic;

namespace Friflo.Json.Fliox.Mapper.Map.Object
{
    /// <summary>
    /// A enumerator for either a <see cref="Dictionary{TKey,TValue}"/> or <see cref="IDictionary{TKey,TValue}"/>
    /// used to prevent heap allocation for an <see cref="IEnumerator{T}"/> in case using a <see cref="Dictionary{TKey,TValue}"/>
    /// <br/>
    /// It is used to enable using a single implementation for both types of Dictionaries
    /// </summary>
    public struct DictionaryEnumerator<TKey, TValue>
    {
        private             Dictionary<TKey,TValue>.Enumerator      dictionaryEnumerator;
        private  readonly   IEnumerator<KeyValuePair<TKey,TValue>>  iDictionaryEnumerator;
        
        public DictionaryEnumerator(IDictionary<TKey,TValue> iDictionary) {
            if (iDictionary is Dictionary<TKey,TValue> dictionary) {
                dictionaryEnumerator = dictionary.GetEnumerator();
                iDictionaryEnumerator = null;
                return;
            }
            iDictionaryEnumerator   = iDictionary.GetEnumerator();
            dictionaryEnumerator    = default;
        }

        public bool MoveNext() {
            if (iDictionaryEnumerator == null)
                return dictionaryEnumerator.MoveNext();
            return iDictionaryEnumerator.MoveNext();
        }
        
        public KeyValuePair<TKey, TValue> Current { get {
            if (iDictionaryEnumerator == null)
                return dictionaryEnumerator.Current;
            return iDictionaryEnumerator.Current;
        } }
    }
}