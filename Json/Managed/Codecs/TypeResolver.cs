using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class TypeResolver
    {
        private readonly TypeStore typeStore;
        
        public TypeResolver(TypeStore typeStore) {
            this.typeStore = typeStore;
        }

        public  NativeType Create (FieldInfo field) {
            return Create (field. FieldType);
        }

        public  NativeType Create (PropertyInfo getter) {
            return Create (getter. PropertyType);
        }

        public NativeType CreateCollection (Type type) {
            return Create (type);
        }

        private NativeType Create(Type type) {
            NativeType nativeType = CreateType(type);
            typeStore.typeMap.Put(type, nativeType);
            return nativeType;
        }

        private NativeType CreateType (Type type)
        {
            // If retrieving element type gets to buggy, change retrieving element type of collections.
            // In this case element type have to be specified via:
            // void Property.Set(String name, Class<?> entryType)
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return new TypeNotSupported(type);

                SimpleType.Id? id = SimpleType.IdFromType(elementType);
                var ar = GetArrayResolver(id);
                return new PropCollection(typeof(Array), type, elementType, ar, type.GetArrayRank(), null);
            }

            Type[] args;
            args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                return new PropCollection  ( typeof( IList<>), type, elementType, ListCodec.Resolver, 1, null);
            }

            args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
            if (args != null)
            {
                Type elementType = args[1];
                IJsonCodec or = MapCodec.Resolver;
                return new PropCollection  ( typeof( IDictionary<,> ), type, elementType, or, 1, args[0]);
            }
            
            if (type.IsClass) {
                return new PropType(this, type);
            }
            
            if (type.IsValueType) {
                return new PropType(this, type);
            }

            return null;
        }

        static IJsonCodec GetArrayResolver(SimpleType.Id? id) {
            switch (id) {
                case SimpleType.Id.String:  return StringArrayCodec.Resolver;
                case SimpleType.Id.Long:    return LongArrayCodec.Resolver;
                case SimpleType.Id.Integer: return IntArrayCodec.Resolver;
                case SimpleType.Id.Short:   return ShortArrayCodec.Resolver;
                case SimpleType.Id.Byte:    return ByteArrayCodec.Resolver;
                case SimpleType.Id.Bool:    return BoolArrayCodec.Resolver;
                case SimpleType.Id.Double:  return DoubleArrayCodec.Resolver;
                case SimpleType.Id.Float:   return FloatArrayCodec.Resolver;
                case SimpleType.Id.Object:  return ObjectArrayCodec.Resolver;
                default:
                    throw new NotSupportedException("unsupported array type: " + id.ToString());
            }
        }
    }
}