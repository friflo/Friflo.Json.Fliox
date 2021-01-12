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
        private   readonly  HashMapLang <Type,  NativeType>       typeMap=    new HashMapLang <Type,  NativeType >();
        internal  readonly  HashMapLang <Bytes, NativeType>       nameMap=    new HashMapLang <Bytes, NativeType >();
            
        public void Dispose() {
            lock (nameMap) {
                foreach (var type in typeMap.Values)
                    type.Dispose();
            }
        }

       
        internal NativeType GetType (Type type, String name)
        {
            lock (typeMap)
            {
                NativeType propType = typeMap.Get(type);
                if (propType == null)
                {
                    PropCollection propCollection = PropCollection.Info.CreateCollection(type);
                    if (propCollection != null) {
                        propType = propCollection;
                    }
                    else if (type.IsClass) {
                        propType = new PropType(type, name);
                    }
                    else if (type.IsValueType) {
                        propType = new PropType(type, name);
                    }
                    else {
                        throw new NotSupportedException($"Type not supported: " + type.FullName);
                    }
                    typeMap.Put(type, propType);
                }
                return propType;
            }
        }
            
        public void RegisterType (String name, Type type)
        {
            lock (nameMap)
            {
                NativeType nativeType = GetType(type, name);
                if (nativeType is PropType) {
                    PropType propType = (PropType)nativeType;
                    if (!propType.typeName.buffer.IsCreated())
                        throw new FrifloException("Type already created without registered name");
                    if (!propType.typeName.IsEqualString(name))
                        throw new FrifloException("Type already registered with different name: " + name);
                    nameMap.Put(propType.typeName, propType);
                }
            }
        }

    }
}