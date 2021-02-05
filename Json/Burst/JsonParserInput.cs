using System;
using System.Text;

namespace Friflo.Json.Burst
{
    public partial struct JsonParser
    {
        enum InputType {
            ByteArray,
        }
        
        public string DebugString { get {
                int start = pos - 20;
                if (start < 0)
                    start = 0;
                int end = pos + 30;
                if (end > bufEnd)
                    end = bufEnd;
                var builder = new StringBuilder();
                int n = start;
                for (; n < end; n++) {
                    if (n == pos)
                        builder.Append(" => ");
                    builder.Append((char) buf.buffer.array[n]);
                }
                if (pos == bufEnd)
                    builder.Append(" => <end of buffer>");
                
                return $"pos: ({pos})  {builder}";
            }
        }
        
        public void InitParser(Bytes bytes) {
            InitParser (bytes.buffer.array, bytes.start, bytes.Len);
        }

        /// <summary>
        /// Before starting iterating a JSON document the parser need be initialized with the document to parse.
        /// </summary>
        /// <param name="bytes">The JSON document to parse</param>
        /// <param name="start">The start position in bytes inside <see cref="bytes"/> where parsing starts.</param>
        /// <param name="len">The length of bytes inside <see cref="bytes"/> which are intended to parse.</param>
        public void InitParser(byte[] bytes, int start, int len) {
            inputType       = InputType.ByteArray;
            inputArray      = bytes;
            inputArrayPos   = start;
            inputArrayEnd   = start + len;
            Start();
        }

        private bool Read() {
            if (pos != bufEnd)
                throw new InvalidOperationException("expect pos != bufEnd in Read() pos: " + pos);


            bool success;
            
            switch (inputType) {
                case InputType.ByteArray:
                    success = ReadByteArray();
                    break;
                default:
                    throw new NotImplementedException("inputType: " + inputType);
            }

            if (success) {
                bufferCount += pos;
                pos = 0;
                return true;
            }
            return false;
        }
        
        private bool ReadByteArray() {
            if (inputArrayPos == inputArrayEnd)
                return false;
            
            int curPos = inputArrayPos;
            inputArrayPos += BufSize;
            if (inputArrayPos > inputArrayEnd)
                inputArrayPos = inputArrayEnd;
            
            int len = inputArrayPos - curPos;
            Buffer.BlockCopy(inputArray, curPos, buf.buffer.array, 0, len);
            bufEnd = len;
            return true;
        }

    }
}


