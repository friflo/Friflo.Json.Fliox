using System;
using System.Collections.Generic;


namespace Friflo.Json.Managed.Prop.Resolver
{
    public class ReadJsonArrayResolver
    {
        public Func<JsonReader, object, NativeType, object> GetReadResolver(NativeType nativeType) {
            return null;
        }
    }
}