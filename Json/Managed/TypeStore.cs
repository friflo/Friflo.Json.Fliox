using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed
{
    /// <summary>
    /// Thread safe store containing the required <see cref="Type"/> information for marshalling and unmarshalling.
    /// Can be shared across threads by <see cref="JsonReader"/> and <see cref="JsonWriter"/> instances.
    /// </summary>
    public class TypeStore : IDisposable
    {
        private   readonly  HashMapLang <Type, PropType>    typeMap=    new HashMapLang <Type, PropType >();
        internal  readonly  HashMapLang <Bytes, PropType>   nameMap=    new HashMapLang <Bytes, PropType >();
            
        public void Dispose() {
            lock (nameMap) {
                foreach (var type in typeMap.Values)
                    type.Dispose();
            }
        }
            
        internal PropType GetInternal (Type type, String name)
        {
            lock (typeMap)
            {
                PropType propType = typeMap.Get(type);
                if (propType == null)
                {
                    propType = new PropType(type, name);
                    typeMap.Put(type, propType);
                }
                return propType;
            }
        }
            
        public void RegisterType (String name, Type type)
        {
            lock (nameMap)
            {
                PropType propType = GetInternal(type, name); 
                if (!propType.typeName.buffer.IsCreated())
                    throw new FrifloException("Type already created without registered name");
                if (!propType.typeName.IsEqualString(name))
                    throw new FrifloException("Type already registered with different name: " + name);
                nameMap.Put(propType.typeName, propType);
            }
        }

    }
}