using System;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Prop;
using Friflo.Json.Managed.Utils;

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
            while (true) {
                switch (reader.parser.Event) {
                    case JsonEvent.ValueString:
                        return BigInteger.Parse(reader.parser.value.ToString());
                    case JsonEvent.Error:
                        return null;
                    default:
                        return null;
                }
            }
        }
    }
}
