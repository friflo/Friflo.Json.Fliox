// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;

namespace Friflo.Json.Managed.Utils
{
	// HashMapLang
	public class HashMapLang<K,V> : Dictionary <K,V> , FFMap<K,V>
	{
		public HashMapLang()
		{
		}

		public HashMapLang (int capacity)
		:
			base (capacity) {
		}

		public int Size()
		{
			return Count;
		}

		public new bool Remove(K k)
		{
			return base.Remove(k);
		}

		public void Put(K k, V v)
		{
			base[k] = v;
		}

		public V Get(K k)
		{
			TryGetValue (k, out V val);
			return val;
		}

		public new bool ContainsKey(K k)
		{
			return base.ContainsKey(k);
		}

		public void Rehash(int newCap)
		{
		}

//		public void Clear()
//		{
//			clear();
//		}

		public override String ToString()
		{
			return SysUtil.ToString (this);
		}
	}
}
