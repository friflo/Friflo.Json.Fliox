using System;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Codecs;
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
        internal  readonly  HashMapLang <Type,  NativeType> typeMap=        new HashMapLang <Type,  NativeType >();
        //
        internal  readonly  HashMapLang <Bytes, NativeType> nameToType=     new HashMapLang <Bytes, NativeType >();
        internal  readonly  HashMapLang <Type,  Bytes>      typeToName =    new HashMapLang <Type,  Bytes >();

        private   readonly TypeResolver typeResolver;

        public TypeStore() {
            typeResolver = new TypeResolver(this);
        }
            
        public void Dispose() {
            lock (nameToType) {
                foreach (var item in typeMap.Values)
                    item.Dispose();
                foreach (var item in typeToName.Values)
                    item.Dispose();
            }
        }

        internal NativeType GetType (Type type)
        {
            lock (this)
            {
                NativeType nativeType = typeResolver.GetNativeType(type);
                if (nativeType != null)
                    return nativeType;
                
                throw new NotSupportedException($"Type not supported: " + type.FullName);
            }
        }
            
        public void RegisterType (String name, Type type)
        {
            using (var bytesName = new Bytes(name)) {
                lock (this) {
                    NativeType nativeType = nameToType.Get(bytesName);
                    if (nativeType != null) {
                        if (type != nativeType.type)
                            throw new InvalidOperationException("Another type is already registered with this name: " + name);
                        return;
                    }
                    nativeType = GetType(type);
                    Bytes discriminator = new Bytes(name);
                    typeToName.Put(nativeType.type, discriminator);
                    nameToType.Put(discriminator, nativeType);
                }
            }
        }

    }
}