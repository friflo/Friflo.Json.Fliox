using System;
using System.Collections;


namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ReadJsonObjectResolver
    {
        public Func<JsonReader, object, PropType, object> GetReadResolver(PropType propType) {
            return null;
        }
    }
}