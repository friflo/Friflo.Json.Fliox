using System;

namespace Friflo.Json.Burst
{
    public partial struct JsonParser
    {
        private InputType inputType;
        
        enum InputType {
            ByteArray,
        }
        
        public void InitParser(Bytes bytes) {
            InitParser (bytes.buffer.array, bytes.start, bytes.Len);
        }

        public void InitParser(byte[] bytes, int start, int len) {
            inputType = InputType.ByteArray;
            buf.InitBytes(len);
            buf.EnsureCapacityAbs(len);
            Buffer.BlockCopy(bytes, 0, buf.buffer.array, 0, len);
            this.bufEnd = len;
            Start();
        }


    }
}