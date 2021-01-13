using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed.Codecs
{
    public interface IJsonCodec {
        object  Read  (JsonReader reader, object obj, NativeType nativeType);
        void    Write (JsonWriter writer, object obj, NativeType nativeType);
    }
}