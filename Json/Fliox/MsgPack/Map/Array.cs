// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackArray<T>
    {
        internal static void ReadMsg (ref T[] array, ref MsgReader reader)
        {
            if (!reader.ReadArray(out int length)) {
                array = null;
                return;
            }
            if (array == null || array.Length != length) {
                array = new T[length];
            }
            T item = default;
            for (int n = 0; n < length; n++) {
                MsgPackMapper<T>.Instance.read(ref item, ref reader);
                array[n] = item;
            }
        }
        
        internal static void WriteMsg(ref T[] array, ref MsgWriter writer) {
            if (array == null) {
                writer.WriteNull();
                return;
            }
            int length = array.Length;
            writer.WriteArray(length);
            for (int n = 0; n < length; n++) {
                MsgPackMapper<T>.Instance.write(ref array[n], ref writer);
            }
        }
    }
}