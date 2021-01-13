using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;


namespace Friflo.Json.Managed.Codecs
{
    public class BigIntCodec : IJsonCodec
    {
        public static readonly BigIntCodec Resolver = new BigIntCodec();
        
        public NativeType CreateHandler(TypeResolver resolver, Type type) {
            if (type != typeof(BigInteger))
                return null;
            return new NativeType (typeof(BigInteger), Resolver);
        }
        
        public void Write (JsonWriter writer, object obj, NativeType nativeType) {
            BigInteger value = (BigInteger) obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToString());
            writer.bytes.AppendChar('\"');
        }

        public Object Read(JsonReader reader, Object obj, NativeType nativeType) {
            if (reader.parser.Event == JsonEvent.ValueString) { 
                if (BigInteger.TryParse(reader.parser.value.ToString(), out BigInteger ret))
                    return ret;
            }
            return null;
        }
    }
}
