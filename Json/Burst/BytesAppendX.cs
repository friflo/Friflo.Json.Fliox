namespace Friflo.Json.Burst
{
    public partial struct Bytes
    {
        private void AppendBytes1(ref Bytes src) {
            
        }
        
        private unsafe void AppendBytes6(ref Bytes src) {
            fixed (byte* srcPtr = &src.buffer.array[src.start])
            fixed (byte* destPtr = &buffer.array[end]) {
                *(int*)(destPtr + 0) = *(int*)(srcPtr + 0);
                *(int*)(destPtr + 2) = *(int*)(srcPtr + 2);
            }
            end += 6;
        } 
        
        private unsafe void AppendBytes9(ref Bytes src) {
            fixed (byte* srcPtr = &src.buffer.array[src.start])
            fixed (byte* destPtr = &buffer.array[end]) {
                *(int*)(destPtr + 0) = *(int*)(srcPtr + 0);
                *(int*)(destPtr + 4) = *(int*)(srcPtr + 4);
                *(int*)(destPtr + 5) = *(int*)(srcPtr + 5);
            }
            end += 9;
        }
        
        private unsafe void AppendBytes12(ref Bytes src) {
            fixed (byte* srcPtr = &src.buffer.array[src.start])
            fixed (byte* destPtr = &buffer.array[end]) {
                *(int*)(destPtr + 0) = *(int*)(srcPtr + 0);
                *(int*)(destPtr + 4) = *(int*)(srcPtr + 4);
                *(int*)(destPtr + 8) = *(int*)(srcPtr + 8);
            }
            end += 12;
        } 
        
    }
}