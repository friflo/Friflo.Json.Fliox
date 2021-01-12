using System;
using System.Collections;


namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ReadJsonObjectResolver
    {
        public Func<JsonReader, object, PropType, object> GetReadResolver(PropType propType) {
            /*if (typeof(IDictionary).IsAssignableFrom(propType.nativeType)) { //typeof( IDictionary<,> )
                return JsonReader.ReadMapType;
            } */
            return null;
        }
    }
}