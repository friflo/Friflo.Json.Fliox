using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    public class NativeType : IDisposable {
        public  readonly    Type        type;
        public  readonly    IJsonCodec  codec;

        public virtual Object CreateInstance() {
            return null;
        }

        public NativeType(Type type, IJsonCodec codec) {
            this.type = type;
            this.codec = codec;
        }

        public virtual void Dispose() {
        }
    }

}