using System;
using System.Collections.Generic;

namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ReadJsonArrayResolver
    {
        public Func<JsonReader, object, PropCollection, object> GetReadResolver(PropCollection collection) {
            if (collection.typeInterface == typeof(Array)) {
                if (collection.rank > 1)
                    throw new NotSupportedException("multidimensional arrays not supported. Type" + collection.type);
                switch (collection.id) {
                    case SimpleType.Id.String:  return ArrayReadResolver.ReadArrayString;
                    case SimpleType.Id.Long:    return ArrayReadResolver.ReadArrayLong;
                    case SimpleType.Id.Integer: return ArrayReadResolver.ReadArrayInt;
                    case SimpleType.Id.Short:   return ArrayReadResolver.ReadArrayShort;
                    case SimpleType.Id.Byte:    return ArrayReadResolver.ReadArrayByte;
                    case SimpleType.Id.Bool:    return ArrayReadResolver.ReadArrayBool;
                    case SimpleType.Id.Double:  return ArrayReadResolver.ReadArrayDouble;
                    case SimpleType.Id.Float:   return ArrayReadResolver.ReadArrayFloat;
                    case SimpleType.Id.Object:  return ArrayReadResolver.ReadArrayObject;
                    default:
                        throw new NotSupportedException("unsupported array type: " + collection.id.ToString());
                }
            }
            
            if (collection.typeInterface == typeof( IList<> ))
                return JsonReader.ReadList;
            return null;
        }
    }
}