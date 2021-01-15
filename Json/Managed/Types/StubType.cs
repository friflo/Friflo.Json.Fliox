using System;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Types
{
    // todo make StubType and InitStubType() abstract
    public class StubType : IDisposable {
        public  readonly    Type        type;
        public  readonly    IJsonCodec  codec;

        public virtual Object CreateInstance() {
            return null;
        }

        public StubType(Type type, IJsonCodec codec) {
            this.type = type;
            this.codec = codec;
        }

        public virtual void Dispose() {
        }
        
        public static bool IsStandardType(Type type) {
            return type.IsPrimitive || type == typeof(string) || type.IsArray;
        }
        
        public static bool IsGenericType(Type type) {
            while (type != null) {
                if (type.IsConstructedGenericType)
                    return true;
                type = type.BaseType;
            }
            return false;
        } 

        public virtual void InitStubType(TypeStore typeStore) {
            
        }
    }

}