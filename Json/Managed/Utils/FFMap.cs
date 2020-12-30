
namespace Friflo.Json.Managed.Utils
{
	public interface FFMap<K,V>
	{
		int		Size		();
		bool	Remove		(K k);
		void 	Put			(K k, V v);
		V 		Get			(K k);
		bool 	ContainsKey (K k);
		void 	Rehash		(int newCap);
		void 	Clear		();
	}
}
