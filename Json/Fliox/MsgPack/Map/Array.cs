// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackArray
    {
        internal static void ReadMsg<T> (ref MsgReader reader, ref T[] array)
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
                MsgPackMapper<T>.Instance.read(ref reader, ref item);
                array[n] = item;
            }
        }
        
        internal static void WriteMsg<T>(ref MsgWriter writer, ref T[] array) {
            if (array == null) {
                writer.WriteNull();
                return;
            }
            int length = array.Length;
            writer.WriteArray(length);
            for (int n = 0; n < length; n++) {
                MsgPackMapper<T>.Instance.write(ref writer, ref array[n]);
            }
        }
    }
}